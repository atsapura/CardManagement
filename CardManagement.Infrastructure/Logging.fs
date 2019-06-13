namespace CardManagement.Infrastructure

module Logging =
    open CardManagement.Common
    open Serilog
    open Errors
    open ErrorMessages

    let private funcFinishedWithError funcName = sprintf "%s finished with error: %s" funcName

    let logDataError funcName e =
        let errorMessage = dataRelatedErrorMessage e |> funcFinishedWithError funcName
        match e with
        | InvalidDbData d -> Log.Error errorMessage
        | _ -> Log.Warning errorMessage

    let logValidationError funcName e = validationMessage e |> funcFinishedWithError funcName |> Log.Information

    let logOperationNotAllowed funcName e = operationNotAllowedMessage e |> funcFinishedWithError funcName |> Log.Warning

    let logError funcName e =
        match e with
        | DataError e -> logDataError funcName e
        | ValidationError e -> logValidationError funcName e
        | OperationNotAllowed e -> logOperationNotAllowed funcName e
        | Bug _ ->
            let errorMessage = errorMessage e
            Log.Error(errorMessage)

    let logify funcName func x =
        sprintf "start %s with arg\n%A" funcName x |> Log.Information
        let result = func x
        sprintf "%s finished with result\n%A" funcName result |> Log.Information
        result

    let logifyAsync funcName funcAsync x =
        async {
            sprintf "start %s with arg\n%A" funcName x |> Log.Information
            let! result = funcAsync x
            match result with
            | Ok ok -> sprintf "%s finished with result\n%A" funcName ok |> Log.Information
            | Error e ->
                match box e with
                | :? DataRelatedError as er -> logDataError funcName er
                | :? Error as er -> logError funcName er
                | :? ValidationError as er -> logValidationError funcName er
                | :? OperationNotAllowedError as er -> logOperationNotAllowed funcName er
                | e -> sprintf "%A" e |> Log.Error
            return result
        }
