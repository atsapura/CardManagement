namespace CardManagement.Common

module Errors =
    open System

    type ValidationError =
        { FieldPath: string
          Message: string }

    type OperationNotAllowedError =
        { Operation: string
          Reason: string }

    type DataRelatedError =
        | EntityNotFound of entityName: string * id: string
        | EntityIsInUse of entityName: string
        | UpdateError of entityName:string * message:string
        | Panic of Exception

    type Error =
        | ValidationError of ValidationError
        | OperationNotAllowed of OperationNotAllowedError
        | DataError of DataRelatedError
        | Bug of Exception

    let validationError fieldPath message = { FieldPath = fieldPath; Message = message } |> Error

    let bug exc = Bug exc |> Error

    let operationNotAllowed operation reason = { Operation = operation; Reason = reason } |> Error

    let notFound name id = EntityNotFound (name, id) |> Error

    let entityInUse name = EntityIsInUse name |> Error

    let expectValidationError result = Result.mapError ValidationError result

    let expectOperationNotAllowedError result = Result.mapError OperationNotAllowed result

    let expectDataRelatedError result =
        match result with
        | Error (Panic ex) -> Bug ex |> Error
        | result -> Result.mapError DataError result

    let expectDataRelatedErrorAsync asyncResult =
        async {
            let! result = asyncResult
            return expectDataRelatedError result
        }

    type AsyncResult<'a, 'error> = Async<Result<'a, 'error>>
    type ValidationResult<'a> = Result<'a, ValidationError>
    type IoResult<'a> = AsyncResult<'a, DataRelatedError>
    type PipelineResult<'a> = AsyncResult<'a, Error>
