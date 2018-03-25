module Json

open System.Text
open Newtonsoft.Json

let private enc = Encoding.UTF8

let fromBytes<'a> (bytes:byte array) =
  try
    bytes
    |> enc.GetString
    |> fun json ->
      printfn "Json: %s" json
      json
    |> JsonConvert.DeserializeObject<'a>
    |> Choice1Of2
  with e -> Choice2Of2 e

let toBytes value =
  value
  :> obj
  |> JsonConvert.SerializeObject
  |> enc.GetBytes

let toString value =
  value
  :> obj
  |> JsonConvert.SerializeObject