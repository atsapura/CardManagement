namespace CardManagement

(* Finally this is our composition of domain functions. In here we build those execution trees.
   If you want to see, how we inject dependencies in here, go to 
   `CardManagement.Infrastructure.CardProgramInterpreter`. *)
module CardWorkflow =
    open CardDomain
    open System
    open CardDomainCommandModels
    open CardManagement.Common
    open CardDomainQueryModels
    open Errors
    open CardProgramBuilder

    let private noneToError (a: 'a option) id =
        let error = EntityNotFound (sprintf "%sEntity" typeof<'a>.Name, id)
        Result.ofOption error a

    let private tryGetCard cardNumber =
        program {
            let! card = getCardByNumber cardNumber
            let! card = noneToError card cardNumber.Value |> expectDataRelatedError
            return Ok card
        }

    let processPayment (currentDate: DateTimeOffset, payment) =
        program {
            (* You can see these `expectValidationError` and `expectDataRelatedErrors` functions here.
               What they do is map different errors into `Error` type, since every execution branch
               must return the same type, in this case `Result<'a, Error>`.
               They also help you quickly understand what's going on in every line of code:
               validation, logic or calling external storage. *)
            let! cmd = validateProcessPaymentCommand payment |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let today = currentDate.Date |> DateTimeOffset
            let tomorrow = currentDate.Date.AddDays 1. |> DateTimeOffset
            let! operations = getBalanceOperations (cmd.CardNumber, today, tomorrow)
            let spentToday = BalanceOperation.spentAtDate currentDate cmd.CardNumber operations
            let! (card, op) =
                CardActions.processPayment currentDate spentToday card cmd.PaymentAmount
                |> expectOperationNotAllowedError
            do! saveBalanceOperation op |> expectDataRelatedErrorProgram
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let setDailyLimit (currentDate: DateTimeOffset, setDailyLimitCommand) =
        program {
            let! cmd = validateSetDailyLimitCommand setDailyLimitCommand |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let! card = CardActions.setDailyLimit currentDate cmd.DailyLimit card |> expectOperationNotAllowedError
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let topUp (currentDate: DateTimeOffset, topUpCmd) =
        program {
            let! cmd = validateTopUpCommand topUpCmd |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let! (card, op) = CardActions.topUp currentDate card cmd.TopUpAmount |> expectOperationNotAllowedError
            do! saveBalanceOperation op |> expectDataRelatedErrorProgram
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let activateCard activateCmd =
        program {
            let! cmd = validateActivateCardCommand activateCmd |> expectValidationError
            let! result = getCardWithAccountInfo cmd.CardNumber
            let! (card, accInfo) = noneToError result cmd.CardNumber.Value |> expectDataRelatedError
            let card = CardActions.activate accInfo card
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let deactivateCard deactivateCmd =
        program {
            let! cmd = validateDeactivateCardCommand deactivateCmd |> expectValidationError
            let! card = tryGetCard cmd.CardNumber
            let card = CardActions.deactivate card
            do! replaceCard card |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let createUser (userId, createUserCommand) =
        program {
            let! userInfo = validateCreateUserCommand userId createUserCommand |> expectValidationError
            do! createNewUser userInfo |> expectDataRelatedErrorProgram
            return
                { UserInfo = userInfo
                  Cards = Set.empty } |> toUserModel |> Ok
        }

    let createCard cardCommand =
        program {
            let! card = validateCreateCardCommand cardCommand |> expectValidationError
            let accountInfo = AccountInfo.Default cardCommand.UserId
            do! createNewCard (card, accountInfo) |> expectDataRelatedErrorProgram
            return card |> toCardInfoModel |> Ok
        }

    let getCard cardNumber =
        program {
            let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
            let! card = getCardByNumber cardNumber
            return card |> Option.map toCardInfoModel |> Ok
        }

    let getUser userId =
        simpleProgram {
            let! maybeUser = getUserById userId
            return maybeUser |> Option.map toUserModel
        }
