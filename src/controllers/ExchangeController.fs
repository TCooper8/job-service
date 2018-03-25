module ExchangeController

open Suave
open Suave.Filters
open Suave.Operators

open ExchangeModel

let private post service: WebPart =
  Controller.jsonReq<ExchangeInput> (fun input http ->
    ExchangeService.post service input
    |> Controller.writeAsyncResult http
  )

let private get service id: WebPart =
  fun http ->
    ExchangeService.get service id
    |> Controller.writeAsyncResult http

let app service =
  choose
    [ POST >=> path "/exchanges" >=> post service
      GET >=> pathScan "/exchanges/%s" (get service)
    ]
