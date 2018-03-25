module JobModel

open System
open Results

type JobId = string
type JobInfo = {
  /// <summary>The id of job.</summary>
  id: JobId
  createdOn: DateTime
  dueBy: DateTime
  priority: uint8
  acceptedOn: DateTime option
  updatedOn: DateTime option
  completedOn: DateTime option
  exchange: string
  subject: string
  contentType: string
  body: byte array
}

type JobInput = {
  dueBy: DateTime
  priority: uint8
  exchange: string
  subject: string
  contentType: string
  body: byte array
}

type ListInput = {
  exchange: string
  subjectLike: string
  offset: int64
  limit: uint8
}

[<Interface>]
type IJobService =
  abstract Post: JobInput -> unit Result Async
  abstract Get: string -> JobInfo Result Async
  abstract List: ListInput -> JobInfo seq Result Async

module JobService =
  let private map mapping (service:IJobService) =
    mapping service

  let post service input = map (fun s -> s.Post input) service
  let get service id = map (fun s -> s.Get id) service
  let list service input = map (fun s -> s.List input) service