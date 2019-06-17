// Learn more about F# at http://fsharp.org

open System

    open CardManagement.CardDomainCommandModels
    open CardManagement.Infrastructure
    open CardManagement

[<EntryPoint>]
let main argv =
    AppConfiguration.configureLog()
    let userId = Guid.Parse "b3f0a6f4-ee04-48ab-b838-9b3330c6bca9"
    let cardNumber = "1234123412341234"
    //let createCard =
    //    { CreateCardCommandModel.CardNumber = cardNumber
    //      ExpirationMonth = 11us
    //      ExpirationYear = 2023us
    //      Name = "Roman Roman"
    //      UserId = userId }
    //let card = CompositionRoot.createCard createCard |> Async.RunSynchronously
    //let user = CompositionRoot.getUser userId |> Async.RunSynchronously
    //let card = CompositionRoot.getCard cardNumber |> Async.RunSynchronously
    //let setDailyLimitModel =
    //    { SetDailyLimitCardCommandModel.UserId = userId
    //      Number = cardNumber
    //      Limit = 500M}
    //let setLimitResult = CompositionRoot.setDailyLimit setDailyLimitModel |> Async.RunSynchronously
    let paymentModel =
        { ProcessPaymentCommandModel.Number = cardNumber
          UserId = userId
          PaymentAmount = 400M}
    //let paymentResult = CompositionRoot.processPayment paymentModel |> Async.RunSynchronously
    let result =
        CardWorkflow.processPayment DateTimeOffset.UtcNow paymentModel
        |> Interpreter.interpretCardProgram
        |> Async.RunSynchronously
    printfn "FINISHED!\n%A" result
    Console.ReadLine() |> ignore
    0 // return an integer exit code
