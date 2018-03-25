module ExchangeService

open AsyncUtil
open System
open ExchangeModel
open Results

let private postQuery = """
  insert into exchanges (
    name
  )
  values (
    :name
  )
  returning id;
"""

let private getQuery = """
  select
    exchange.id,
    exchange.name,
    (select count(*)
      from jobs job
      where job.exchange_id = exchange.id
    )
  from exchanges exchange
  where exchange.id = :id
"""

let private getByNameQuery = """
  select
    exchange.id,
    exchange.name,
    (select count(*)
      from jobs job
      where job.exchange_id = exchange.id
    )
  from exchanges exchange
  where exchange.name = :name
"""

let private post host (input:ExchangeInput) =
  Sql.Ast.Prepared(
    postQuery,
    [ "name", input.name :> obj
    ]
  )
  |> Sql.Ast.asScalar host
  |> Async.map (string)
  |> Async.map (Created)

let private get host id =
  match Guid.TryParse id with
    | false, _ ->
      Sql.Ast.Prepared(
        getByNameQuery,
        [ "name", id :> obj
        ]
      )
    | true, id ->
      Sql.Ast.Prepared(
        getQuery,
        [ "id", id :> obj
        ]
      )
  |> Sql.Ast.asReader host
  |> Async.bind (fun reader -> async {
    let! reader = Sql.Reader.read reader
    match reader with
    | None -> return NotFound "Exchange not found"
    | Some reader ->
      let! id = Sql.Reader.ord reader 0
      let! name = Sql.Reader.ord reader 1
      let! jobCount = Sql.Reader.ord reader 2

      return
        Ok
          { id = id
            name = name
            jobCount = jobCount
          }
  })


type private Service (host) =
  interface IExchangeService with
    member __.Post input = post host input
    member __.Get id = get host id

let init host =
  Service host
  :> IExchangeService
