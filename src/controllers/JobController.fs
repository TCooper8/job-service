module JobController

open Suave
open Suave.Filters
open Suave.Operators

open JobModel

let private post service: WebPart =
  Controller.jsonReq (fun input http ->
    JobService.post service input
    |> Controller.writeAsyncResult http
  )

let private get service id: WebPart =
  fun http ->
    JobService.get service id
    |> Controller.writeAsyncResult http

let private listInput (http:HttpContext) =
  try
    let exchange =
      match http.request.queryParam "exchange" with
      | Choice2Of2 _ -> failwithf "Expected `exchange` query to be defined"
      | Choice1Of2 value -> value

    let subjectLike =
      match http.request.queryParam "subjectLike" with
      | Choice2Of2 _ -> "*"
      | Choice1Of2 value -> value

    { exchange = exchange
      subjectLike = subjectLike
      offset = 0L
      limit = 100uy
    }
    |> Choice1Of2
  with e -> Choice2Of2 e

let private list service: WebPart =
  fun http ->
    match listInput http with
    | Choice2Of2 e -> RequestErrors.BAD_REQUEST e.Message http
    | Choice1Of2 input ->
      JobService.list service input
      |> Controller.writeAsyncResult http

let app service =
  choose
    [ POST >=> path "/jobs" >=> post service
      GET >=> path "/jobs" >=> list service
      GET >=> pathScan "/jobs/%s" (get service)
    ]