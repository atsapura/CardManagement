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
    type GetCard        = CardNumberString              -> PipelineResult<CardInfoModel>
    type GetCardDetails = CardNumberString              -> PipelineResult<CardDetailsModel>
    type GetUser        = UserId                        -> PipelineResult<UserModel>
    type ActivateCard   = ActivateCardCommandModel      -> PipelineResult<CardInfoModel>
    type DeactivateCard = DeactivateCardCommandModel    -> PipelineResult<CardInfoModel>
    type SetDailyLimit  = SetDailyLimitCardCommandModel -> PipelineResult<CardInfoModel>
    type ProcessPayment = ProcessPaymentCommandModel    -> PipelineResult<CardInfoModel>

    let getCard (getCardAsync: CardNumber -> IoResult<Card>) : GetCard =
        fun cardNumber ->
            asyncResult {
                // let! is like `await` in C#, but more powerful: when `await` basically works only with `Task`,
                // "let!" works the same way with everything you make it work: `Result`, `Async`, `Task`, `AsyncResult` etc.
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                // those functions `expectValidationError` and `expectDataRelatedErrorAsync`
                // map specific error types like `ValidationError` to on final `Error`, otherwise code won't compile.
                // they also tell you what kind of error you should expect on every step. Nice!
                let! card = getCardAsync cardNumber |> expectDataRelatedErrorAsync
                return card |> toCardInfoModel
            }

    let getCardDetails (getCardDetailsAsync: CardNumber -> IoResult<CardDetails>)
        : GetCardDetails =
        fun cardNumber ->
            asyncResult {
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                let! card = getCardDetailsAsync cardNumber |> expectDataRelatedErrorAsync
                return card |> toCardDetailsModel
            }

    let getUser (getUserAsync: UserId -> IoResult<User>) : GetUser =
        fun userId ->
            asyncResult {
                let! user =
                    getUserAsync userId
                    |> expectDataRelatedErrorAsync
                return user |> toUserModel
            }

    let activateCard
        (getCardAsync: CardNumber -> IoResult<Card>)
        (getCardAccountInfoAsync: CardNumber -> IoResult<AccountInfo>)
        : ActivateCard =
        fun activateCommand ->
            asyncResult {
                let! validCommand =
                    activateCommand |> validateActivateCardCommand |> expectValidationError
                let! card = validCommand.CardNumber |> getCardAsync |> expectDataRelatedErrorAsync
                let! activeCard =
                    match card.AccountDetails with
                    | Active _ -> card |> AsyncResult.retn
                    | Deactivated ->
                        asyncResult {
                            let! cardAccountInfo =
                                getCardAccountInfoAsync validCommand.CardNumber
                                |> expectDataRelatedErrorAsync
                            return activate cardAccountInfo card
                        }
                return activeCard |> toCardInfoModel
            }

    let deactivateCard
        (getCardAsync: CardNumber -> IoResult<Card>)
        : DeactivateCard =
        fun deactivateCommand ->
            asyncResult {
                let! validCommand =
                    deactivateCommand |> validateDeactivateCardCommand
                    |> expectValidationError
                let! card =
                    validCommand.CardNumber |> getCardAsync |> expectDataRelatedErrorAsync
                let deactivatedCard = deactivate card
                return deactivatedCard |> toCardInfoModel
            }

    let setDailyLimit
        (currentDate: DateTimeOffset)
        (getCardAsync: CardNumber -> IoResult<Card>)
        : SetDailyLimit =
            fun setDailyLimitCommand ->
                asyncResult {
                    let! validCommand =
                        setDailyLimitCommand |> validateSetDailyLimitCommand |> expectValidationError
                    let! card =
                        getCardAsync validCommand.CardNumber
                        |> expectDataRelatedErrorAsync
                    let! updatedCard =
                        setDailyLimit currentDate validCommand.DailyLimit card
                        |> expectOperationNotAllowedError
                    return updatedCard |> toCardInfoModel
                }

    let processPayment
        (currentDate: DateTimeOffset)
        (getCardAsync: CardNumber -> IoResult<Card>)
        (getSpentTodayAsync: CardNumber -> IoResult<Money>)
        (saveSpentTodayAsync: CardNumber -> Money -> IoResult<unit>)
        : ProcessPayment =
            fun processCommand ->
                asyncResult {
                    let! validCommand =
                        processCommand |> validateProcessPaymentCommand |> expectValidationError
                    let cardNumber = validCommand.CardNumber
                    let! card = getCardAsync cardNumber |> expectDataRelatedErrorAsync
                    let! spentToday = getSpentTodayAsync cardNumber |> expectDataRelatedErrorAsync
                    let! (card, newSpentToday) =
                        processPayment currentDate spentToday card validCommand.PaymentAmount
                        |> expectOperationNotAllowedError
                    do! saveSpentTodayAsync cardNumber newSpentToday |> expectDataRelatedErrorAsync
                    return card |> toCardInfoModel
                }
