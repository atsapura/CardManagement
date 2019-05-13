namespace CardManagement

open CardDomainReadModels
open CardManagement.Common
open CardDomainCommandModels
open CardDomain
open FsToolkit.ErrorHandling

open Errors

module CardWorkflows =
    open CardActions
    open System

    type AsyncResult<'a, 'error> = Async<Result<'a, 'error>>
    type CardNumberString = string
    type GetCard = CardNumberString -> AsyncResult<CardInfoModel, Error>
    type GetCardDetails = CardNumberString -> AsyncResult<CardDetailsModel, Error>
    type GetUser = UserId -> AsyncResult<UserModel, Error>
    type ActivateCard = ActivateCardCommandModel -> AsyncResult<CardInfoModel, Error>
    type DeactivateCard = DeactivateCardCommandModel -> AsyncResult<CardInfoModel, Error>
    type SetDailyLimit = SetDailyLimitCardCommandModel -> AsyncResult<CardInfoModel, Error>

    let getCard (getCardFromDbAsync: CardNumber -> AsyncResult<Card, Error>) : GetCard =
        fun cardNumber ->
            asyncResult {
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                let! card = getCardFromDbAsync cardNumber
                return card |> toCardInfoModel
            }

    let getCardDetails (getCardDetailsAsync: CardNumber -> AsyncResult<CardDetails, DataRelatedError>)
        : GetCardDetails =
        fun cardNumber ->
            asyncResult {
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                let! card = getCardDetailsAsync cardNumber |> expectDataRelatedErrorAsync
                return card |> toCardDetailsModel
            }

    let getUser (getUserAsync: UserId -> AsyncResult<User, Error>) : GetUser =
        fun userId ->
            asyncResult {
                let! user = getUserAsync userId
                return user |> toUserModel
            }

    let activateCard
        (getCardAsync: CardNumber -> AsyncResult<Card, DataRelatedError>)
        (getCardAccountInfoAsync: CardNumber -> AsyncResult<CardAccountInfo, DataRelatedError>)
        : ActivateCard =
        fun activateCommand ->
            asyncResult{
                let! validCommand =
                    activateCommand |> validateActivateCardCommand |> expectValidationError
                let! card = validCommand.CardNumber |> getCardAsync |> expectDataRelatedErrorAsync
                let! activeCard =
                    match card with
                    | Active _ -> card |> AsyncResult.retn
                    | Deactivated _ ->
                        asyncResult {
                            let! cardAccountInfo =
                                getCardAccountInfoAsync validCommand.CardNumber
                                |> expectDataRelatedErrorAsync
                            return activate cardAccountInfo card
                        }
                return activeCard |> toCardInfoModel
            }

    let deactivateCard
        (getCardAsync: CardNumber -> AsyncResult<Card, DataRelatedError>)
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
        (getCardFromDbAsync: CardNumber -> AsyncResult<Card, DataRelatedError>)
        : SetDailyLimit =
            fun setDailyLimitCommand ->
                asyncResult {
                    let! validCommand =
                        setDailyLimitCommand |> validateSetDailyLimitCommand |> expectValidationError
                    let! card =
                        getCardFromDbAsync validCommand.CardNumber
                        |> expectDataRelatedErrorAsync
                    let! updatedCard =
                        setDailyLimit currentDate validCommand.DailyLimit card
                        |> expectOperationNotAllowedError
                    return updatedCard |> toCardInfoModel
                }
