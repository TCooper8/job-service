module ReportController

open System

open Suave
open Suave.Filters
open Suave.Operators

open ReportModel

type PostReport = {
  page: int32 Nullable
  title: string
  body: byte array
}

let private post service jobId: WebPart =
  Controller.jsonReq<PostReport> (fun input http ->
    let input =
      if input.page.HasValue then
        { jobId = jobId
          page = Some input.page.Value
          title = input.title
          body = input.body
        }
      else
        { jobId = jobId
          page = None
          title = input.title
          body = input.body
        }
    ReportService.post service input
    |> Controller.writeAsyncResult http
  )

let private listInput (http:HttpContext) jobId =
  try
    { jobId = jobId
      offset = 0L
      limit = 100uy
    }
    |> Choice1Of2
  with e -> Choice2Of2 e

let private list service jobId: WebPart =
  fun http ->
    match listInput http jobId with
    | Choice2Of2 e -> RequestErrors.BAD_REQUEST e.Message http
    | Choice1Of2 input ->
      ReportService.list service input
      |> Controller.writeAsyncResult http

let app service =
  choose
    [ POST >=> pathScan "/jobs/%s/reports" (post service)
      GET >=> pathScan "/jobs/%s/reports" (list service)
    ]
