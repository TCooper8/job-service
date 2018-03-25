module AsyncUtil

module Async =
  let map mapping task = async {
    let! res = task
    return mapping res
  }

  let bind binding task = async {
    let! res = task
    return! binding res
  }

  let ofTask task = Async.AwaitTask

  let mapSome mapping task = async {
    let! res = task
    match res with
    | None -> return None
    | Some res ->
      let res = mapping res
      return Some res
  }

  let bindSome binding task = async {
    let! res = task
    match res with
    | None -> return None
    | Some res ->
      let! res = binding res
      return Some res
  }

  let sync = Async.RunSynchronously