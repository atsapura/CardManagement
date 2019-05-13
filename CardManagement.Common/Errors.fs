namespace CardManagement.Common

module Errors =
    open System

    type Error =
        | ValidationError of message:string
        | OperationNotAllowed of message:string
        | EntityNotFound of entityName: string * id: string
        | EntityIsInUse of entityName: string
        | Bug of Exception

    let validationError message = ValidationError message |> Error

    let bug exc = Bug exc |> Error

    let operationNotAllowed message = OperationNotAllowed message |> Error

    let notFound name id = EntityNotFound (name, id) |> Error

    let entityInUse name = EntityIsInUse name |> Error
