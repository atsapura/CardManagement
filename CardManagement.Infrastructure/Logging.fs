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
        printf "start %s with arg %A" funcName x
        let result = func x
        printf "%s finished with result %A" funcName result
        result

    let logifyAsync funcName funcAsync x =
        async {
            printf "start %s with arg %A" funcName x
            let! result = funcAsync x
            printf "%s finished with result %A" funcName result
            match result with
            | Ok ok -> printf "%s finished with result %A" funcName ok
            | Error e ->
                match box e with
                | :? DataRelatedError as er -> logDataError er
                | :? Error as er -> logError er
                | :? ValidationError as er -> logValidationError er
                | :? OperationNotAllowedError as er -> logOperationNotAllowed er
                | e -> sprintf "%A" e |> Log.Error
            return result
        }
