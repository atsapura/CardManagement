namespace CardManagement.Infrastructure

module CardProgramInterpreter =
    open CardManagement
    open CardManagement.Common
    open Logging
    open CardWorkflow
    open CardManagement.Data

    let private mongoSettings() = AppConfiguration.buildConfig() |> AppConfiguration.getMongoSettings
    let private getMongoDb() = mongoSettings() |> CardMongoConfiguration.getDatabase

    let private getCardAsync mongoDb =
        CardDataPipeline.getCardAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getCardAsync"

    let private getUserAsync mongoDb =
        CardDataPipeline.getUserWithCards mongoDb |> logifyPlainAsync "CardDataPipeline.getUserWithCardsAsync"

    let private getCardWithAccInfoAsync mongoDb =
        CardDataPipeline.getCardWithAccountInfoAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getCardWithAccountInfoAsync"

    let private replaceCardAsync mongoDb =
        CardDataPipeline.replaceCardAsync mongoDb |> logifyResultAsync "CardDataPipeline.replaceCardAsync"

    let private getBalanceOperationsAsync mongoDb =
        CardDataPipeline.getBalanceOperationsAsync mongoDb |> logifyPlainAsync "CardDataPipeline.getBalanceOperationsAsync"

    let private saveBalanceOperationAsync mongoDb =
        CardDataPipeline.createBalanceOperationAsync mongoDb |> logifyResultAsync "CardDataPipeline.createBalanceOperationAsync"

    let private createCardAsync mongoDb =
        CardDataPipeline.createCardAsync mongoDb |> logifyResultAsync "CardPipeline.createCardAsync"

    let private createUserAsync mongoDb =
        CardDataPipeline.createUserAsync mongoDb |> logifyResultAsync "CardDataPipeline.createUserAsync"

    let rec private interpretCardProgram mongoDb prog =
        match prog with
        | GetCard (cardNumber, next) ->
            cardNumber |> getCardAsync mongoDb |> mapAsync (next >> interpretCardProgram mongoDb)
        | GetCardWithAccountInfo (number, next) ->
            number |> getCardWithAccInfoAsync mongoDb |> mapAsync (next >> interpretCardProgram mongoDb)
        | CreateCard ((card,acc), next) ->
            (card, acc) |> createCardAsync mongoDb |> mapAsync (next >> interpretCardProgram mongoDb)
        | ReplaceCard (card, next) ->
            card |> replaceCardAsync mongoDb |> mapAsync (next >> interpretCardProgram mongoDb)
        | GetUser (id, next) ->
            getUserAsync mongoDb id |> mapAsync (next >> interpretCardProgram mongoDb)
        | CreateUser (user, next) ->
            user |> createUserAsync mongoDb |> mapAsync (next >> interpretCardProgram mongoDb)
        | GetBalanceOperations (request, next) ->
            getBalanceOperationsAsync mongoDb request |> mapAsync (next >> interpretCardProgram mongoDb)
        | SaveBalanceOperation (op, next) ->
             saveBalanceOperationAsync mongoDb op |> mapAsync (next >> interpretCardProgram mongoDb)
        | Stop a -> async.Return a

    let interpret funcName prog =
        let interpret = interpretCardProgram (getMongoDb()) |> logifyResultAsync funcName
        interpret prog

    let interpretSimple funcName prog =
        let interpret = interpretCardProgram (getMongoDb()) |> logifyPlainAsync funcName
        interpret prog
