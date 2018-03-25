module ReportModel

open System
open Results
open JobModel

type ReportInfo = {
  page: int32
  createdOn: DateTime
  title: string
  body: byte array
}

type ReportInput = {
  jobId: string
  page: int32 option
  title: string
  body: byte array
}

type RangeInput = {
  offset: int64
  limit: int8
}

type ListInput = {
  jobId: string
  offset: int64
  limit: uint8
}

[<Interface>]
type IReportService =
  abstract Post: ReportInput -> unit Result Async
  abstract List: ListInput -> ReportInfo seq Result Async

module ReportService =
  let private map mapping (service:IReportService) =
    mapping service
  let post service input = map (fun s -> s.Post input) service
  let list service input = map (fun s -> s.List input) service