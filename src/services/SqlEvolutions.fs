module DbEvolutions

open System
open System.IO
open Sql
open AsyncUtil
open Sql.Ast

type private Evolution = {
  id: int
}

let private createEvolutions =
  """
    create table if not exists evolutions (
      id int primary key
    );
  """

let private selectEvolution =
  """
    select
      id
    from evolutions
    where id=:id
  """

let private insertEvolution =
  """
    insert into evolutions (
      id
    )
    values (
      :id
    )
  """

type private Evolutions (connectionString) =
  member this.Init () =
    createEvolutions
    |> Cmd.cmd
    |> Cmd.thenConnect connectionString
    |> Async.bind Cmd.exec

  member this.Get id =
    Prepared(
      selectEvolution,
      [ "id", id :> obj
      ]
    )
    |> asReader connectionString
    |> Async.bind Reader.read
    |> Async.bindSome (fun reader -> async {
      use reader = reader
      let! id = Reader.ord reader 0
      let record =
        { id = id
        }
      return record
    })

  member this.Put input =
    Prepared(
      insertEvolution,
      [ "id", input.id :> obj
      ]
    )
    |> exec connectionString

  member this.Apply (data:string) =
    Query data
    |> transaction connectionString

let private (|Int|_|) str =
  match Int32.TryParse str with
  | true, i -> Some i
  | _ -> None

let private fileIndex (info:FileInfo) =
  info.Name
  |> fun n -> n.Replace("up.sql", "")
  |> (|Int|_|)

let evolve connectionString rootDir = async {
  // Setup the evolutions
  do!
    Query createEvolutions
    |> exec connectionString
    |> Async.Ignore

  // Now we need to gather all of the `up` files and order them.
  let service = Evolutions connectionString
  let dirInfo = DirectoryInfo rootDir
  let upFiles =
    dirInfo.EnumerateFiles("*up.sql")
    |> Seq.cast<FileInfo>
    |> Seq.choose (fun info ->
      match fileIndex info with
      | None -> None
      | Some i -> Some(info, i)
    )
    |> Seq.sortBy snd

  for file, id in upFiles do
    printfn "INFO Checking evolution %i" id
    // First, we will check and see if the evolution exists.
    let! record = service.Get id
    match record with
    | Some _ ->
      printfn "INFO Evolution %i already exists" id
      // Evolution already exists, skip it.
      ()
    | None ->
      printfn "INFO Applying evolution %i..." id
      let! data = File.ReadAllTextAsync file.FullName |> Async.AwaitTask
      let! _ = service.Apply data
      do! service.Put { id = id } |> Async.Ignore
      printfn "INFO Evolution %i applied" id
      ()
  ()
}