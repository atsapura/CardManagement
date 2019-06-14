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
        CardDataPipeline.getCardAsync mongoDb |> logifyAsync "CardDataPipeline.getCardAsync"

    let private getCardWithAccInfoAsync mongoDb =
        CardDataPipeline.getCardWithAccountInfoAsync mongoDb |> logifyAsync "CardDataPipeline.getCardWithAccountInfoAsync"

    let private replaceCardAsync mongoDb =
        CardDataPipeline.replaceCardAsync mongoDb |> logifyAsync "CardDataPipeline.replaceCardAsync"

    let private getBalanceOperationsAsync mongoDb =
        CardDataPipeline.getBalanceOperationsAsync mongoDb |> logifyAsync "CardDataPipeline.getBalanceOperationsAsync"

    let private createBalanceOperationAsync mongoDb =
        CardDataPipeline.createBalanceOperationAsync mongoDb |> logifyAsync "CardDataPipeline.createBalanceOperationAsync"

    let createUser : CreateUser =
        fun userModel ->
        let mongoDb = getMongoDb()
        let userId = Guid.NewGuid()
        let createUserAsync = CardDataPipeline.createUserAsync mongoDb |> logifyAsync "CardDataPipeline.createUserAsync"
        userModel
        |> (CardPipeline.createUser userId createUserAsync |> logifyAsync "CardPipeline.createUser")

    let getUser : GetUser =
        fun userId ->
        let mongoDb = getMongoDb()
        let getUserAsync = CardDataPipeline.getUserWithCards mongoDb |> logifyAsync "CardDataPipeline.getUserWithCardsAsync"
        userId
        |> (CardPipeline.getUser getUserAsync |> logifyAsync "CardPipeline.getCard")

    let createCard : CreateCard =
        fun cardModel ->
        let mongoDb = getMongoDb()
        let createCardAsync = CardDataPipeline.createCardAsync mongoDb |> logifyAsync "CardDataPipeline.createCardAsync"
        cardModel
        |> (CardPipeline.createCard createCardAsync |> logifyAsync "CardPipeline.createCard")

    let getCard : GetCard =
        fun cardNumber ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        cardNumber
        |> (CardPipeline.getCard getCardAsync |> logifyAsync "CardPipeline.getCard")

    let activateCard : ActivateCard =
        fun activateCardCmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardWithAccInfoAsync mongoDb 
        let replaceCardAsync = replaceCardAsync mongoDb
        activateCardCmd
        |> (CardPipeline.activateCard getCardAsync replaceCardAsync |> logifyAsync "CardPipeline.activateCard")

    let deactivateCard : DeactivateCard =
        fun deactivateCardCmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        deactivateCardCmd
        |> (CardPipeline.deactivateCard getCardAsync replaceCardAsync |> logifyAsync "CardPipeline.deactivateCard")

    let setDailyLimit : SetDailyLimit =
        fun cmd ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        cmd
        |> (CardPipeline.setDailyLimit DateTimeOffset.UtcNow getCardAsync replaceCardAsync
            |> logifyAsync "CardPipeline.getCard")

    let processPayment : ProcessPayment =
        fun paymentModel ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let getBalanceOperationsAsync = getBalanceOperationsAsync mongoDb
        let createBalanceOperationAsync = createBalanceOperationAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        paymentModel
        |> (CardPipeline.processPayment
                DateTimeOffset.UtcNow
                getCardAsync
                getBalanceOperationsAsync
                replaceCardAsync
                createBalanceOperationAsync
            |> logifyAsync "CardPipeline.processPayment")

    let topUp : TopUp =
        fun topUpModel ->
        let mongoDb = getMongoDb()
        let getCardAsync = getCardAsync mongoDb
        let createBalanceOperationAsync = createBalanceOperationAsync mongoDb
        let replaceCardAsync = replaceCardAsync mongoDb
        topUpModel
        |> (CardPipeline.topUp DateTimeOffset.UtcNow getCardAsync replaceCardAsync createBalanceOperationAsync
            |> logifyAsync "CardPipeline.topUp")
