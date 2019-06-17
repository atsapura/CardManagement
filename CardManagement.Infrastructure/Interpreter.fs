namespace CardManagement.Infrastructure

module Interpreter =
    open CardManagement
    open CardManagement.Common
    open Errors
    open Logging
    open CardWorkflow
    open CardManagement.Data
    open FsToolkit.ErrorHandling

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

    let rec interpretCardProgram prog =
        let mongoDb = getMongoDb()
        match prog with
        | GetCard (cardNumber, next) ->
            async {
                let! maybeCard = cardNumber |> getCardAsync mongoDb
                return! maybeCard |> next |> interpretCardProgram 
            }
        | GetCardWithAccountInfo (number, next) ->
            async {
                let! maybe = number |> getCardWithAccInfoAsync mongoDb
                return! maybe |> next |> interpretCardProgram
            }
        | CreateCard ((card,acc), next) ->
            async {
                let! createResult = (card, acc) |> createCardAsync mongoDb
                return! createResult |> next |> interpretCardProgram
            }
        | ReplaceCard (card, next) ->
            async {
                let! result = card |> replaceCardAsync mongoDb
                return! result |> next |> interpretCardProgram
            }
        | GetUser (id, next) ->
            async {
                let! maybeUser = getUserAsync mongoDb id
                return! maybeUser |> next |> interpretCardProgram
            }
        | CreateUser (user, next) ->
            async {
                let! result = user |> createUserAsync mongoDb
                return! result |> next |> interpretCardProgram
            }
        | GetBalanceOperations (request, next) ->
            async {
                let! operations = getBalanceOperationsAsync mongoDb request
                return! operations |> next |> interpretCardProgram
            }
        | SaveBalanceOperation (op, next) ->
            async {
                let! result = saveBalanceOperationAsync mongoDb op
                return! result |> next |> interpretCardProgram
            }
        | Stop a -> async.Return a
