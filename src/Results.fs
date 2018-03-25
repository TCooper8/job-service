module Results

type 'a Result =
  | Ok of 'a
  | Created of string
  | NoContent
  | BadRequest of string
  | NotFound of string
  | InternalError of exn