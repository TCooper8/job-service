module Controller

open System
open Suave
open Suave.Operators
open Results

let private writeJson value =
  Successful.OK (Json.toString value)
  >=> Writers.setHeader "content-type" "application/json"

let writeResult (http:HttpContext) value =
  printfn "Result %A" value
  value
  |> function
  | Ok value -> writeJson value http
  | Created id -> Successful.CREATED id http
  | NoContent -> Successful.NO_CONTENT http
  | BadRequest msg -> RequestErrors.BAD_REQUEST msg http
  | NotFound msg -> RequestErrors.NOT_FOUND msg http
  | InternalError e ->
      let id = Guid.NewGuid() |> string
      printfn "Error %s %A" id e
      ServerErrors.INTERNAL_ERROR id http

let writeAsyncResult (http:HttpContext) task = async {
  let! res = Async.Catch task
  let res =
    match res with
    | Choice1Of2 res -> res
    | Choice2Of2 e -> InternalError e
  return! writeResult http res
}

let routeNotFound: WebPart =
  fun http ->
    RequestErrors.NOT_FOUND "Invalid route" http

let jsonReq<'a> next: WebPart =
  fun http ->
    Json.fromBytes<'a> http.request.rawForm
    |> function
      | Choice2Of2 e ->
        printfn "Invalid json %A" e
        printfn "Msg = %s" e.Message
        RequestErrors.BAD_REQUEST e.Message http
      | Choice1Of2 input ->
        next input http