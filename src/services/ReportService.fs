module ReportService

open AsyncUtil
open ReportModel
open Results

let private postPageQuery = """
  with updated as (
    update jobs set
      updated_on = (now() at time zone 'utc')
    where id = :job_id::uuid
    returning updated_on
  )
  insert into reports (
    job_id,
    page,
    created_on,
    title,
    body
  )
  values (
    :job_id::uuid,
    :page,
    (select updated_on from updated),
    :title,
    :body
  )
"""

let private postQuery = """
  with updated as (
    update jobs set
      updated_on = (now() at time zone 'utc')
    where id = :job_id::uuid
    returning updated_on
  )
  insert into reports (
    job_id,
    page,
    created_on,
    title,
    body
  )
  values (
    :job_id::uuid,
    (select coalesce(max(page), 0) from reports where job_id = :job_id::uuid) + 1,
    (select updated_on from updated),
    :title,
    :body
  )
"""

let private listQuery = """
  select
    page,
    created_on,
    title,
    body
  from reports
  where job_id=:job_id::uuid
  order by page desc
  offset :offset
  limit :limit
"""

let private post host (input:ReportInput) =
  let page, query =
    match input.page with
    | None -> 0, postQuery
    | Some page -> page, postPageQuery

  Sql.Ast.Prepared(
    query,
    [ "job_id", input.jobId :> obj
      "page", page :> obj
      "title", input.title :> obj
      "body", input.body :> obj
    ]
  )
  |> Sql.Ast.exec host
  |> Async.map (fun _ -> NoContent)

let private readRecord reader = async {
  let! page = Sql.Reader.field reader "page"
  let! createdOn = Sql.Reader.field reader "created_on"
  let! title = Sql.Reader.field reader "title"
  let! body = Sql.Reader.field reader "body"

  return
    { page = Option.get page
      createdOn = Option.get createdOn
      title = Option.get title
      body = Option.get body
    }
}

let private list host (input:ListInput) =
  Sql.Ast.Prepared(
    listQuery,
    [ "job_id", input.jobId :> obj
      "offset", input.offset :> obj
      "limit", input.limit :> obj
    ]
  )
  |> Sql.Ast.asReader host
  |> Async.bind (fun reader -> async {
    let rec loop acc = async {
      let! reader = Sql.Reader.read reader
      match reader with
      | None -> return acc
      | Some reader ->
        let! record = readRecord reader
        return! loop (record::acc)
    }
    let! acc = loop []
    return seq acc
  })
  |> Async.map Ok

type private Service (host) =
  interface IReportService with
    member __.Post input = post host input
    member __.List input = list host input

let init host =
  Service host
  :> IReportService