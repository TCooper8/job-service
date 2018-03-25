module Sql

open Npgsql
open System.Data.Common
open AsyncUtil

[<AutoOpen>]
module Cmd =
  let connect connectionString = async {
    let conn = new NpgsqlConnection(connectionString)
    do! conn.OpenAsync() |> Async.AwaitTask
    return conn
  }

  let cmd sql =
    printfn "DEBUG Creating cmd from %s" sql
    new NpgsqlCommand(sql)

  let withParam (key:string) (value:obj) (cmd:NpgsqlCommand) =
    printfn "DEBUG Adding sql param %s %A" key value
    do cmd.Parameters.AddWithValue(key, value) |> ignore
    cmd

  let withParams (pairs: (string * obj) seq) (cmd:NpgsqlCommand) =
    pairs
    |> Seq.fold (fun cmd (key, value) -> withParam key value cmd) cmd

  let withConn conn (cmd:NpgsqlCommand) =
    cmd.Connection <- conn
    cmd

  let thenConnect connectionString (cmd:NpgsqlCommand) = async {
    printfn "DEBUG Connecting to %s" connectionString
    let! conn = connect connectionString
    return withConn conn cmd
  }

  let asReader (cmd:NpgsqlCommand) =
    cmd.ExecuteReaderAsync()
    |> Async.AwaitTask

  let asScalar (cmd:NpgsqlCommand) =
    cmd.ExecuteScalarAsync()
    |> Async.AwaitTask
    |> fun task -> async {
      let! res = task
      return res :?> 'a
    }

  let exec (cmd:NpgsqlCommand) =
    cmd.ExecuteNonQueryAsync()
    |> Async.AwaitTask

module Reader =
  let field (reader:DbDataReader) name = async {
    let ord = reader.GetOrdinal name
    let! isNull = reader.IsDBNullAsync ord |> Async.AwaitTask
    if isNull then return None
    else
      let! value = reader.GetFieldValueAsync ord |> Async.AwaitTask
      return Some value
  }

  let ord (reader:DbDataReader) index =
    reader.GetFieldValueAsync index
    |> Async.AwaitTask

  let read (reader:DbDataReader) = async {
    let! ok = reader.ReadAsync() |> Async.AwaitTask
    if not ok then
      reader.Dispose()
      return None
    else return Some reader
  }

module Ast =
  type Ast =
    | Query of string
    | Prepared of string * (string * obj) seq

  let exec connectionString = function
    | Query queryString ->
      queryString
      |> cmd
      |> thenConnect connectionString
      |> Async.bind Cmd.exec

    | Prepared (queryString, ps) ->
      queryString
      |> cmd
      |> withParams ps
      |> thenConnect connectionString
      |> Async.bind Cmd.exec

  let asReader connectionString = function
    | Query queryString ->
      queryString
      |> cmd
      |> thenConnect connectionString
      |> Async.bind Cmd.asReader

    | Prepared (queryString, ps) ->
      queryString
      |> cmd
      |> withParams ps
      |> thenConnect connectionString
      |> Async.bind Cmd.asReader

  let asScalar connectionString = function
    | Query queryString ->
      queryString
      |> cmd
      |> thenConnect connectionString
      |> Async.bind Cmd.asScalar

    | Prepared (queryString, ps) ->
      queryString
      |> cmd
      |> withParams ps
      |> thenConnect connectionString
      |> Async.bind Cmd.asScalar

  let transaction connectionString = function
    | Query queryString ->
      queryString
      |> cmd
      |> thenConnect connectionString
      |> Async.bind (fun cmd -> async {
        let transaction = cmd.Connection.BeginTransaction()
        let! res = cmd |> Cmd.exec |> Async.Catch
        match res with
        | Choice2Of2 e ->
          do! transaction.RollbackAsync() |> Async.AwaitTask
          raise e
          return -1
        | Choice1Of2 res ->
          do! transaction.CommitAsync() |> Async.AwaitTask
          return res
      })

    | Prepared (queryString, ps) ->
      queryString
      |> cmd
      |> withParams ps
      |> thenConnect connectionString
      |> Async.bind (fun cmd -> async {
        let transaction = cmd.Connection.BeginTransaction()
        let! res = cmd |> Cmd.exec |> Async.Catch
        match res with
        | Choice2Of2 e ->
          do! transaction.RollbackAsync() |> Async.AwaitTask
          raise e
          return -1
        | Choice1Of2 res ->
          do! transaction.CommitAsync() |> Async.AwaitTask
          return res
      })
