namespace CardManagement.Infrastructure

module Logging =
    open CardManagement.Common
    open Serilog
    open Errors
    open ErrorMessages

    let logDataError e =
        let errorMessage = dataRelatedErrorMessage e
        match e with
        | InvalidDbData d -> Log.Error errorMessage
        | _ -> Log.Warning errorMessage

    let logValidationError e = validationMessage e |> Log.Information

    let logOperationNotAllowed e = operationNotAllowedMessage e |> Log.Warning

    let logError e =
        match e with
        | DataError e -> logDataError e
        | ValidationError e -> logValidationError e
        | OperationNotAllowed e -> logOperationNotAllowed e
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
            sprintf "%s finished with result\n%A" funcName result |> Log.Information
            match result with
            | Ok ok -> sprintf "%s finished with result\n%A" funcName ok |> Log.Information
            | Error e ->
                match box e with
                | :? DataRelatedError as er -> logDataError er
                | :? Error as er -> logError er
                | :? ValidationError as er -> logValidationError er
                | :? OperationNotAllowedError as er -> logOperationNotAllowed er
                | e -> sprintf "%A" e |> Log.Error
            return result
        }
