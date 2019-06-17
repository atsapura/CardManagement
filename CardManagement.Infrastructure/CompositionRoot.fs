namespace CardManagement.Infrastructure
(*
This is it. The final point, ultimate composition root of everything there is.
Note, that since we are using `Result<'TOk, 'TError>`, it doesn't break application execution,
unlike exceptions. This means that we can take care of error handling in here and use this root
with whatever interface we deem fit: web api, console app, desctop app: just use functions from
this module, provide input and it will do the rest.
The only thing you have to take care of is `Exception`s, which now finally really represent that
something went wrong and it's an unpredictable dangerous situation,
and not something like "Oh boy, user provided invalid data".
*)
module CompositionRoot =
    open CardManagement
    open CardManagement.Common
    open Errors
    open Logging

    open CardDomain
    open CardDomainCommandModels
    open CardManagement.CardDomainQueryModels
    open CardManagement.Data
    open System

    type CardNumberString = string

    type CreateUser = CreateUserCommandModel -> PipelineResult<UserModel>
    type CreateCard = CreateCardCommandModel -> PipelineResult<CardInfoModel>
    type SetDailyLimit = SetDailyLimitCardCommandModel -> PipelineResult<CardInfoModel>
    type ProcessPayment = ProcessPaymentCommandModel -> PipelineResult<CardInfoModel>
    type GetUser = UserId -> PipelineResult<UserModel option>
    type GetCard = CardNumberString -> PipelineResult<CardInfoModel option>
    type ActivateCard = ActivateCardCommandModel -> PipelineResult<CardInfoModel>
    type DeactivateCard = DeactivateCardCommandModel -> PipelineResult<CardInfoModel>
    type TopUp = TopUpCommandModel -> PipelineResult<CardInfoModel>

    let private mongoSettings() = AppConfiguration.buildConfig() |> AppConfiguration.getMongoSettings
    let private getMongoDb() = mongoSettings() |> CardMongoConfiguration.getDatabase

    let private getCardAsync mongoDb =
        CardDataPipeline.getCardAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getCardAsync"

    let private getCardWithAccInfoAsync mongoDb =
        CardDataPipeline.getCardWithAccountInfoAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getCardWithAccountInfoAsync"

    let private replaceCardAsync mongoDb =
        CardDataPipeline.replaceCardAsync mongoDb |> logifyResultAsync "CardDataPipeline.replaceCardAsync"

    let private getBalanceOperationsAsync mongoDb =
        CardDataPipeline.getBalanceOperationsAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getBalanceOperationsAsync"

    let private createBalanceOperationAsync mongoDb =
        CardDataPipeline.createBalanceOperationAsync mongoDb |> logifyResultAsync "CardDataPipeline.createBalanceOperationAsync"

    let createUser : CreateUser =
        fun userModel ->
        let mongoDb = getMongoDb()
        let userId = Guid.NewGuid()
        let createUserAsync = CardDataPipeline.createUserAsync mongoDb |> logifyResultAsync "CardDataPipeline.createUserAsync"
        userModel
        |> (CardPipeline.createUser userId createUserAsync |> logifyResultAsync "CardPipeline.createUser")

    let getUser : GetUser =
        fun userId ->
        let mongoDb = getMongoDb()
        let getUserAsync = CardDataPipeline.getUserWithCards mongoDb |> logifyPlainAsync "CardDataPipeline.getUserWithCardsAsync"
        userId
        |> (CardPipeline.getUser getUserAsync |> logifyResultAsync "CardPipeline.getCard")

    let createCard : CreateCard =
        fun cardModel ->
        let mongoDb = getMongoDb()
        let createCardAsync = CardDataPipeline.createCardAsync mongoDb |> logifyResultAsync "CardDataPipeline.createCardAsync"
        cardModel
        |> (CardPipeline.createCard createCardAsync |> logifyResultAsync "CardPipeline.createCard")

    let getCard : GetCard =
        fun cardNumber ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        cardNumber
        |> (CardPipeline.getCard getCardAsync |> logifyResultAsync "CardPipeline.getCard")

    let activateCard : ActivateCard =
        fun activateCardCmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardWithAccInfoAsync mongoDb 
        let replaceCardAsync = replaceCardAsync mongoDb
        activateCardCmd
        |> (CardPipeline.activateCard getCardAsync replaceCardAsync |> logifyResultAsync "CardPipeline.activateCard")

    let deactivateCard : DeactivateCard =
        fun deactivateCardCmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        deactivateCardCmd
        |> (CardPipeline.deactivateCard getCardAsync replaceCardAsync |> logifyResultAsync "CardPipeline.deactivateCard")

    let setDailyLimit : SetDailyLimit =
        fun cmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        cmd
        |> (CardPipeline.setDailyLimit getCardAsync replaceCardAsync DateTimeOffset.UtcNow
            |> logifyResultAsync "CardPipeline.getCard")

    let processPayment : ProcessPayment =
        fun paymentModel ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let getBalanceOperationsAsync = getBalanceOperationsAsync mongoDb
        let createBalanceOperationAsync = createBalanceOperationAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        paymentModel
        |> (CardPipeline.processPayment
                getCardAsync
                getBalanceOperationsAsync
                replaceCardAsync
                createBalanceOperationAsync
                DateTimeOffset.UtcNow
            |> logifyResultAsync "CardPipeline.processPayment")

    let topUp : TopUp =
        fun topUpModel ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let createBalanceOperationAsync = createBalanceOperationAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        topUpModel
        |> (CardPipeline.topUp getCardAsync replaceCardAsync createBalanceOperationAsync DateTimeOffset.UtcNow
            |> logifyResultAsync "CardPipeline.topUp")
