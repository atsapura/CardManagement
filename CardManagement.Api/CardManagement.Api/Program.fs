namespace CardManagement.Api

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open CardManagement.Infrastructure
open CardManagement
open Common
open Errors
open ErrorMessages
open Serilog

module Program =
    open FSharp.Control.Tasks.V2
    open CardManagement.CardDomainCommandModels

    let errorToResponse e =
        let message = errorMessage e
        match e with
        | Bug exn ->
            Log.Error(exn.ToString())
            ServerErrors.INTERNAL_ERROR "Oops. Something went wrong"
        | OperationNotAllowed _
        | ValidationError _ -> RequestErrors.UNPROCESSABLE_ENTITY message
        | DataError e ->
            match e with
            | EntityNotFound _ -> RequestErrors.NOT_FOUND message
            | _ -> RequestErrors.UNPROCESSABLE_ENTITY message

    let resultToHttpResponseAsync asyncWorkflow : HttpHandler =
        fun next ctx ->
        task {
            let! result = asyncWorkflow |> Async.StartAsTask
            let responseFn =
                match result with
                | Ok ok -> json ok |> Successful.ok
                | Error e -> errorToResponse e
            return! responseFn next ctx
        }

    let optionToHttpResponseAsync asyncWorkflow : HttpHandler =
        fun next ctx ->
        task {
            let! result = asyncWorkflow |> Async.StartAsTask
            let responseFn =
                match result with
                | Ok (Some ok) -> json ok |> Successful.ok
                | Ok None -> RequestErrors.NOT_FOUND "Not found"
                | Error e -> errorToResponse e
            return! responseFn next ctx
        }
    let bindJsonForRoute<'a> r f = route r >=> bindJson<'a> f
    let webApp =
        choose [
            GET >=>
                choose [
                    routef "/users/%O" (fun userId -> CardApi.getUser userId |> optionToHttpResponseAsync)
                    routef "/cards/%s" (fun cardNumber -> CardApi.getCard cardNumber |> optionToHttpResponseAsync)
                ]
            PATCH >=>
                choose [
                    bindJsonForRoute "/cards/deactivate"
                        (fun cmd -> CardApi.deactivateCard cmd |> resultToHttpResponseAsync)
                    bindJsonForRoute "/cards/activate"
                        (fun cmd -> CardApi.activateCard cmd |> resultToHttpResponseAsync)
                    bindJsonForRoute "/cards/setDailyLimit"
                        (fun cmd -> CardApi.setDailyLimit (DateTimeOffset.UtcNow,cmd) |> resultToHttpResponseAsync)
                    bindJsonForRoute "/cards/processPayment"
                        (fun cmd -> CardApi.processPayment (DateTimeOffset.UtcNow,cmd) |> resultToHttpResponseAsync)
                    bindJsonForRoute "/cards/topUp"
                        (fun cmd -> CardApi.topUp (DateTimeOffset.UtcNow,cmd) |> resultToHttpResponseAsync)
                ]
            POST >=>
                choose [
                    bindJsonForRoute "/users"
                        (fun cmd -> CardApi.createUser (Guid.NewGuid(),cmd) |> resultToHttpResponseAsync)
                    bindJsonForRoute "/cards"
                        (fun cmd -> CardApi.createCard cmd |> resultToHttpResponseAsync)
                ]
            RequestErrors.NOT_FOUND "Not Found"
        ]

    let configureApp (app : IApplicationBuilder) =
        // Add Giraffe to the ASP.NET Core pipeline
        app.UseGiraffe webApp

    let configureServices (services : IServiceCollection) =
        // Add Giraffe dependencies
        services.AddGiraffe() |> ignore

    [<EntryPoint>]
    let main args =
        AppConfiguration.configureLog()
        WebHostBuilder()
            .UseKestrel()
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .Build()
            .Run()

        0
