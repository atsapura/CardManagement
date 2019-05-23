namespace CardManagement.Common

(*
This module is about error handling. One of the problems with exceptions is
they don't appear on function/method signature, so we don't know what to expect
when calling particular method unless we read the code/docs.
So one of the goals here is to make function signatures as descriptive as possible.
That's why we introduce here different types of errors:
- ValidationError: for functions that do only validation and nothing else.
- OperationNotAllowedError: for business logic functions. Sometimes user provides valid data,
  but he wants to do something that can't be done, e.g. paying with credit card with no money on it.
- DataRelatedError: for functions that communicate with data storages and 3rd party APIs.
  Some things can only be checked when you have enough data, you can't validate them just from your code.
- Panic: for something unexpected. This means that something is broken, so it's most likely a bug.
- Error: finally this is a type to gather all possible errors.

-------------------------------------------------------------------------------------------------------
Having different types of errors and exposing them in function signatures gives us a lot of information
about functions purpose: e.g. when function may return you `DataRelatedError`, you know it's about
data access layer and nothing else.
Same thing goes for `OperationNonAllowedError`: this function operates with valid input and checks only
for business rules violations.
And finally, if function returns just `Error`, it must be a composition of the whole pipeline:
some validation, then probably business rules checking, then some calls to data base or something and so on.
*)
module Errors =
    open System

    type ValidationError =
        { FieldPath: string
          Message: string }

    type OperationNotAllowedError =
        { Operation: string
          Reason: string }

    type Panic =
        | Exc of Exception
        | PanicMessage of message: string * source: string

    type InvalidDbDataError =
        { EntityName: string
          EntityId: string
          Message: string }

    type DataRelatedError =
        | EntityNotFound of entityName: string * id: string
        | EntityIsInUse of entityName: string
        | UpdateError of entityName:string * message:string
        | InvalidDbData of InvalidDbDataError

    type Bug =
        | Exn of Exception
        | InvalidDbDataError of InvalidDbDataError

    type Error =
        | ValidationError of ValidationError
        | OperationNotAllowed of OperationNotAllowedError
        | DataError of DataRelatedError
        | Bug of Bug

    let validationError fieldPath message = { FieldPath = fieldPath; Message = message } |> Error

    let bug exc = Bug exc |> Error

    let operationNotAllowed operation reason = { Operation = operation; Reason = reason } |> Error

    let notFound name id = EntityNotFound (name, id) |> Error

    let entityInUse name = EntityIsInUse name |> Error

    let expectValidationError result = Result.mapError ValidationError result

    let expectOperationNotAllowedError result = Result.mapError OperationNotAllowed result

    let expectDataRelatedError result =
        match result with
        | Error (InvalidDbData err) -> InvalidDbDataError err |> Bug |> Error
        | result -> Result.mapError DataError result

    let expectDataRelatedErrorAsync asyncResult =
        async {
            let! result = asyncResult
            return expectDataRelatedError result
        }

    (*
    Some type aliases for making code more readable and for preventing
    typo-kind of mistakes: so you don't devlare a validation function with
    plain `Error` type, for example.
    *)
    type AsyncResult<'a, 'error> = Async<Result<'a, 'error>>
    type ValidationResult<'a> = Result<'a, ValidationError>
    type IoResult<'a> = AsyncResult<'a, DataRelatedError>
    type PipelineResult<'a> = AsyncResult<'a, Error>

[<RequireQualifiedAccess>]
module Result =

    let combine results =
        let rec loop acc results =
            match results with
            | [] -> acc
            | result :: tail ->
                match result with
                | Error e -> Error e
                | Ok ok ->
                    let acc = Result.map (fun oks -> ok :: oks) acc
                    loop acc tail
        loop (Ok []) results
