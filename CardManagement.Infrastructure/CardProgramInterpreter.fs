namespace CardManagement.Infrastructure

module CardProgramInterpreter =
    open CardManagement
    open CardManagement.Common
    open Logging
    open CardProgramBuilder
    open CardManagement.Data
    open Errors

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

    (* Here is where we inject dependencies. Unlike classic IoC container
       it checks that you have all the dependencies in compile time. *)
    let rec private interpretCardProgram mongoDb prog =
        match prog with
        | GetCard (cardNumber, next) ->
            cardNumber |> getCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetCardWithAccountInfo (number, next) ->
            number |> getCardWithAccInfoAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | CreateCard ((card,acc), next) ->
            (card, acc) |> createCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | ReplaceCard (card, next) ->
            card |> replaceCardAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetUser (id, next) ->
            getUserAsync mongoDb id |> bindAsync (next >> interpretCardProgram mongoDb)
        | CreateUser (user, next) ->
            user |> createUserAsync mongoDb |> bindAsync (next >> interpretCardProgram mongoDb)
        | GetBalanceOperations (request, next) ->
            getBalanceOperationsAsync mongoDb request |> bindAsync (next >> interpretCardProgram mongoDb)
        | SaveBalanceOperation (op, next) ->
             saveBalanceOperationAsync mongoDb op |> bindAsync (next >> interpretCardProgram mongoDb)
        | Stop a -> async.Return a

    let interpret prog =
        try
            let interpret = interpretCardProgram (getMongoDb())
            interpret prog
        with
        | failure -> Bug failure |> Error |> async.Return

    let interpretSimple prog =
        try
            let interpret = interpretCardProgram (getMongoDb())
            async {
                let! result = interpret prog
                return Ok result
            }
        with
        | failure -> Bug failure |> Error |> async.Return
