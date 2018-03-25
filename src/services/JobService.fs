module JobService

open AsyncUtil
open System
open JobModel
open Results

let private postQuery = """
  insert into jobs (
    due_by,
    priority,
    exchange_id,
    subject,
    content_type,
    body
  )
  values (
    :due_by,
    :priority,
    :exchange,
    :subject,
    :content_type,
    :body
  )
  returning id;
"""

let private listQuery whereClause =
  sprintf """
    select
      id,
      created_on,
      due_by,
      priority,
      accepted_on,
      updated_on,
      completed_on,
      exchange_id,
      subject,
      content_type,
      body
    from jobs
    where %s
    and subject like :subject
    offset :offset
    limit :limit
  """
  <| whereClause

let private post host (input:JobInput) =
  match Guid.TryParse input.exchange with
  | false, _ ->
    async { return BadRequest "Field `exchange` must be a valid uuid" }
  | true, exchange ->
    Sql.Ast.Prepared(
      postQuery,
      [ "due_by", input.dueBy :> obj
        "priority", input.priority :> obj
        "exchange", exchange :> obj
        "subject", input.subject :> obj
        "content_type", input.contentType :> obj
        "body", input.body :> obj
      ]
    )
    |> Sql.Ast.asScalar host
    |> Async.map (string)
    |> Async.map (Created)


let private get id = async {
  return
    Ok
      { id = id
        createdOn = DateTime.Now
        dueBy = DateTime.Now + TimeSpan.FromDays(30.0)
        priority = 0uy
        acceptedOn = None
        updatedOn = None
        completedOn = None
        exchange = "test"
        subject = "test"
        contentType = "json"
        body = """{ "msg": "test" }""" |> System.Text.Encoding.UTF8.GetBytes
      }
}

let private readJob reader = async {
  let! id = Sql.Reader.field reader "id"
  let! createdOn = Sql.Reader.field reader "created_on"
  let! dueBy = Sql.Reader.field reader "due_by"
  let! priority = Sql.Reader.field reader "priority"
  let! acceptedOn = Sql.Reader.field reader "accepted_on"
  let! updatedOn = Sql.Reader.field reader "updated_on"
  let! completedOn = Sql.Reader.field reader "completed_on"
  let! exchange = Sql.Reader.field reader "exchange_id"
  let! subject = Sql.Reader.field reader "subject"
  let! contentType = Sql.Reader.field reader "content_type"
  let! body = Sql.Reader.field reader "body"

  return
    { id = Option.get id
      createdOn = Option.get createdOn
      dueBy = Option.get dueBy
      priority = Option.get priority
      acceptedOn = acceptedOn
      updatedOn = updatedOn
      completedOn = completedOn
      exchange = Option.get exchange
      subject = Option.get subject
      contentType = Option.get contentType
      body = Option.get body
    }
}

let private list host (input:ListInput) =
  let whereClause =
    match Guid.TryParse input.exchange with
    | false, _ ->
      "exchange_id = (select id from exchanges where name like :exchange)"
    | true, _ ->
      "exchange_id = :exchange"

  Sql.Ast.Prepared(
    listQuery whereClause,
    [ "exchange", input.exchange :> obj
      "offset", input.offset :> obj
      "limit", input.limit :> obj
      "subject", input.subjectLike.Replace("*", "%") :> obj
    ]
  )
  |> Sql.Ast.asReader host
  |> Async.bind (fun reader -> async {
    let rec loop jobs = async {
      let! reader = Sql.Reader.read reader
      match reader with
      | None -> return jobs
      | Some reader ->
        let! job = readJob reader
        return! loop (job::jobs)
    }
    let! jobs = loop []
    return seq jobs
  })
  |> Async.map Ok

type private Service (host) =
  interface IJobService with
    member __.Post input = post host input
    member __.Get id = get id
    member __.List input = list host input

let init host =
  Service host
  :> IJobService