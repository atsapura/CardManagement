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

    //let card = CompositionRoot.createCard createCard |> Async.RunSynchronously
    //let user = CompositionRoot.getUser userId |> Async.RunSynchronously
    //let card = CompositionRoot.getCard cardNumber |> Async.RunSynchronously
    //let setDailyLimitModel =
    //    { SetDailyLimitCardCommandModel.UserId = userId
    //      Number = cardNumber
    //      Limit = 500M}
    //let setLimitResult = CompositionRoot.setDailyLimit setDailyLimitModel |> Async.RunSynchronously
    let createUser =
        { Name = "Daario Naharis"
          Address =
            { Country = "Russia"
              City = "The Great City Of Meereen"
              PostalCode = "12345"
              AddressLine1 = "Putrid Grove"
              AddressLine2 = ""} }
    let createCard =
        { CreateCardCommandModel.CardNumber = cardNumber
          ExpirationMonth = 11us
          ExpirationYear = 2023us
          Name = "Daario Naharis"
          UserId = userId }

    let topUpModel =
        { TopUpCommandModel.Number = cardNumber
          TopUpAmount = 10000m }
    let paymentModel =
        { ProcessPaymentCommandModel.Number = cardNumber
          PaymentAmount = 400M}
    let runWholeThingAsync =
        async {
            let! user = CardWorkflow.createUser userId createUser |> CardProgramInterpreter.interpret "createUser"
            let! card = CardWorkflow.createCard createCard |> CardProgramInterpreter.interpret "createCard"
            let! card = CardWorkflow.topUp DateTimeOffset.UtcNow topUpModel |> CardProgramInterpreter.interpret "topUp"
            let! card = CardWorkflow.processPayment DateTimeOffset.UtcNow paymentModel |> CardProgramInterpreter.interpret "processPayment"
            return ()
        }
    runWholeThingAsync |> Async.RunSynchronously
    Console.ReadLine() |> ignore
    0 // return an integer exit code
