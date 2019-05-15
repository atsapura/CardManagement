namespace CardManagement

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

    type GetCard        = CardNumberString              -> PipelineResult<CardInfoModel>
    type GetCardDetails = CardNumberString              -> PipelineResult<CardDetailsModel>
    type GetUser        = UserId                        -> PipelineResult<UserModel>
    type ActivateCard   = ActivateCardCommandModel      -> PipelineResult<CardInfoModel>
    type DeactivateCard = DeactivateCardCommandModel    -> PipelineResult<CardInfoModel>
    type SetDailyLimit  = SetDailyLimitCardCommandModel -> PipelineResult<CardInfoModel>

    let getCard (getCardFromDbAsync: CardNumber -> IoResult<Card>) : GetCard =
        fun cardNumber ->
            asyncResult {
                let! cardNumber = CardNumber.create "cardNumber" cardNumber |> expectValidationError
                let! card = getCardFromDbAsync cardNumber |> expectDataRelatedErrorAsync
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
        (getCardAccountInfoAsync: CardNumber -> IoResult<CardAccountInfo>)
        : ActivateCard =
        fun activateCommand ->
            asyncResult {
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
        (getCardFromDbAsync: CardNumber -> IoResult<Card>)
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
