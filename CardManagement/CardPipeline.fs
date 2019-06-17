namespace CardManagement

(*
Finally, this module is about composition of everything we implemented before.
Every public function here is responsible for creation of 1 pipeline.
And again, nothing else but composing is going on here.
Bonus is that unlike in C# or Java, you don't compose objects here,
you compose functions, which is very easy. So you don't need a DI framework for that,
everything here is 100% your code which is easy to navigate. And you don't
deal here with dull interfaces like `ICardService` which have 1 implementation and 1 purpose:
make it possible to test your code.
*)
module CardPipeline =
    open System
    open CardActions
    open CardDomainQueryModels
    open CardManagement.Common
    open CardDomainCommandModels
    open CardDomain
    open FsToolkit.ErrorHandling
    open Errors

    type CardNumberString = string

    (*
    PipelineResult<'T> is an alias for Async<Result<'T, Error>> which explicitly tells us
    that every pipeline is asynchronous and it may succeed or it may not. If not, you'll get
    an `Error`, which can be either a `ValidationError` or `OperationNotAllowedError`
    or `DataRelatedError` or `Bug of Exception` which means that if it's not one of previous errors,
    we missed something and something's gone bad.
    Unlike with exceptions, you can see everything you need in your function signature,
    which gives you real encapsulation.
    *)
    type GetCard        = CardNumberString              -> PipelineResult<CardInfoModel option>
    type GetCardDetails = CardNumberString              -> PipelineResult<CardDetailsModel option>
    type GetUser        = UserId                        -> PipelineResult<UserModel option>
    type ActivateCard   = ActivateCardCommandModel      -> PipelineResult<CardInfoModel>
    type DeactivateCard = DeactivateCardCommandModel    -> PipelineResult<CardInfoModel>
    type SetDailyLimit  = SetDailyLimitCardCommandModel -> PipelineResult<CardInfoModel>
    type ProcessPayment = ProcessPaymentCommandModel    -> PipelineResult<CardInfoModel>
    type TopUp          = TopUpCommandModel             -> PipelineResult<CardInfoModel>
    type CreateUser     = CreateUserCommandModel        -> PipelineResult<UserModel>
    type CreateCard     = CreateCardCommandModel        -> PipelineResult<CardInfoModel>

    let private noneToError (a: 'a option) id =
        let error = EntityNotFound (sprintf "%sEntity" typeof<'a>.Name, id)
        Result.ofOption error a

    (*
    IoResult<'T> however tells us that the only expectable error here is `DataRelatedError`.
    So there's no validation going on there nor there's a business logic.
    *)
    let getCard (getCardAsync: CardNumber -> IoQueryResult<Card>) : GetCard =
        fun cardNumber ->
            asyncResult {
                // let! is like `await` in C#, but more powerful: when `await` basically works only with `Task`,
                // "let!" works the same way with everything you make it work: `Result`, `Async`, `Task`, `AsyncResult` etc.
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError

                // those functions `expectValidationError` and `expectDataRelatedErrorAsync`
                // map specific error types like `ValidationError` to on final `Error`, otherwise code won't compile.
                // they also tell you what kind of error you should expect on every step. Nice!
                let! card = getCardAsync cardNumber 
                return card |> Option.map toCardInfoModel
            }

    let getCardDetails (getCardDetailsAsync: CardNumber -> IoResult<CardDetails option>)
        : GetCardDetails =
        fun cardNumber ->
            asyncResult {
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                let! card = getCardDetailsAsync cardNumber |> expectDataRelatedErrorAsync
                return card |> Option.map toCardDetailsModel
            }

    let getUser (getUserAsync: UserId -> IoQueryResult<User>) : GetUser =
        fun userId ->
            asyncResult {
                let! user = getUserAsync userId
                return user |> Option.map toUserModel
            }

    let activateCard
        (getCardWithAccountInfoAsync: CardNumber -> IoQueryResult<(Card*AccountInfo)>)
        (replaceCardAsync: Card -> IoResult<unit>)
        : ActivateCard =
        fun activateCommand ->
            asyncResult {
                let! validCommand =
                    activateCommand |> validateActivateCardCommand |> expectValidationError
                let! maybeCard = validCommand.CardNumber |> getCardWithAccountInfoAsync
                let! (card, accountInfo) = noneToError maybeCard validCommand.CardNumber.Value |> expectDataRelatedError
                let activeCard =
                    match card.AccountDetails with
                    | Active _ -> card
                    | Deactivated -> activate accountInfo card
                do! replaceCardAsync activeCard |> expectDataRelatedErrorAsync
                return activeCard |> toCardInfoModel
            }

    let deactivateCard
        (getCardAsync: CardNumber -> IoQueryResult<Card>)
        (replaceCardAsync: Card -> IoResult<unit>)
        : DeactivateCard =
        fun deactivateCommand ->
            asyncResult {
                let! validCommand =
                    deactivateCommand |> validateDeactivateCardCommand
                    |> expectValidationError
                let! maybeCard = validCommand.CardNumber |> getCardAsync
                let! card = noneToError maybeCard validCommand.CardNumber.Value |> expectDataRelatedError
                let deactivatedCard = deactivate card
                do! replaceCardAsync deactivatedCard |> expectDataRelatedErrorAsync
                return deactivatedCard |> toCardInfoModel
            }

    let setDailyLimit
        (getCardAsync: CardNumber -> IoQueryResult<Card>)
        (replaceCardAsync: Card -> IoResult<unit>)
        (currentDate: DateTimeOffset)
        : SetDailyLimit =
            fun setDailyLimitCommand ->
                asyncResult {
                    let! validCommand =
                        setDailyLimitCommand |> validateSetDailyLimitCommand |> expectValidationError
                    let! maybeCard = getCardAsync validCommand.CardNumber
                    let! card = noneToError maybeCard validCommand.CardNumber.Value |> expectDataRelatedError
                    let! updatedCard =
                        setDailyLimit currentDate validCommand.DailyLimit card
                        |> expectOperationNotAllowedError
                    do! replaceCardAsync updatedCard |> expectDataRelatedErrorAsync
                    return updatedCard |> toCardInfoModel
                }

    let processPayment
        (getCardAsync: CardNumber -> IoQueryResult<Card>)
        (getTodayOperations: CardNumber * DateTimeOffset * DateTimeOffset -> Async<BalanceOperation list>)
        (saveCardAsync: Card -> IoResult<unit>)
        (saveBalanceOperation: BalanceOperation -> IoResult<unit>)
        (currentDate: DateTimeOffset)
        : ProcessPayment =
            fun processCommand ->
                asyncResult {
                    let! validCommand =
                        processCommand |> validateProcessPaymentCommand |> expectValidationError
                    let cardNumber = validCommand.CardNumber
                    let! maybeCard = getCardAsync cardNumber
                    let! card = noneToError maybeCard validCommand.CardNumber.Value |> expectDataRelatedError
                    let today = currentDate.Date |> DateTimeOffset
                    let tomorrow = currentDate.Date.AddDays(1.) |> DateTimeOffset
                    let! todayOperations = getTodayOperations (validCommand.CardNumber, today, tomorrow)
                    let spentToday = BalanceOperation.spentAtDate currentDate validCommand.CardNumber todayOperations
                    let! (card, balanceOperation) =
                        processPayment currentDate spentToday card validCommand.PaymentAmount
                        |> expectOperationNotAllowedError
                    do! saveBalanceOperation balanceOperation |> expectDataRelatedErrorAsync
                    do! saveCardAsync card |> expectDataRelatedErrorAsync
                    return card |> toCardInfoModel
                }

    let topUp
        (getCardAsync: CardNumber -> IoQueryResult<Card>)
        (saveCardAsync: Card -> IoResult<unit>)
        (saveBalanceOperation: BalanceOperation -> IoResult<unit>)
        (currentDate: DateTimeOffset)
        : TopUp =
        fun cmd ->
        asyncResult {
            let! topUpCmd = validateTopUpCommand cmd |> expectValidationError
            let! maybeCard = getCardAsync topUpCmd.CardNumber
            let! card = noneToError maybeCard topUpCmd.CardNumber.Value |> expectDataRelatedError
            let! (updatedCard, balanceOperation) =
                topUp currentDate card topUpCmd.TopUpAmount |> expectOperationNotAllowedError
            do! saveCardAsync updatedCard |> expectDataRelatedErrorAsync
            do! saveBalanceOperation balanceOperation |> expectDataRelatedErrorAsync
            return updatedCard |> toCardInfoModel
        }

    let createUser userId (createUserAsync: UserInfo -> IoResult<unit>)
        : CreateUser =
        fun userModel ->
        asyncResult {
            let! userInfo = validateCreateUserCommand userId userModel |> expectValidationError
            do! createUserAsync userInfo |> expectDataRelatedErrorAsync
            return 
                { UserInfo = userInfo
                  Cards = Set.empty } |> toUserModel
        }

    let createCard (createCardAsync: Card*AccountInfo -> IoResult<unit>)
        : CreateCard =
        fun cardModel ->
        asyncResult {
            let! card = validateCreateCardCommand cardModel |> expectValidationError
            let accInfo = AccountInfo.Default card.HolderId
            do! createCardAsync (card, accInfo) |> expectDataRelatedErrorAsync
            return card |> toCardInfoModel
        }

