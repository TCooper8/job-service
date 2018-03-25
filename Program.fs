open Suave

let app dbHost =
  let exchangeService =
    ExchangeService.init dbHost

  let jobService =
    JobService.init dbHost

  let reportService =
    ReportService.init dbHost

  choose
    [ ExchangeController.app exchangeService
      ReportController.app reportService
      JobController.app jobService
      Controller.routeNotFound
    ]

[<EntryPoint>]
let main _ =
  let dbHost = "Server=127.0.0.1;User Id=postgres;Database=postgres;Port=5432;Password=postgres"
  DbEvolutions.evolve dbHost "sql"
  |> Async.RunSynchronously
  |> printfn "Result %A"

  startWebServer
    { defaultConfig with
        bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 8080 ]
    }
    (app dbHost)

  0