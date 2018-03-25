module ExchangeModel

open Results

type ExchangeInfo = {
  id: string
  name: string
  jobCount: int64
}

type ExchangeInput = {
  name: string
}

[<Interface>]
type IExchangeService =
  abstract Post: ExchangeInput -> unit Result Async
  abstract Get: string -> ExchangeInfo Result Async

module ExchangeService =
  let private map mapping (service:IExchangeService) =
    mapping service

  let post service input = map (fun s -> s.Post input) service
  let get service id = map (fun s -> s.Get id) service