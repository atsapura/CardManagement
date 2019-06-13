// Learn more about F# at http://fsharp.org

open System

    open CardManagement.CardDomainCommandModels
    open CardManagement.Infrastructure

[<EntryPoint>]
let main argv =
    //let createUserCmd =
    //    { CreateUserCommandModel.Name = "Roman"
    //      Address =
    //        { CreateAddressCommandModel.Country = "Russia"
    //          City = "SaintPetersburg"
    //          PostalCode = "12345"
    //          AddressLine1 = "Rubinstein st. 13"
    //          AddressLine2 = "ap. 1" } }

    //printfn "creating user:\n%A\n" createUserCmd
    //let user = CompositionRoot.createUser createUserCmd |> Async.RunSynchronously
    //printfn "user created:\n%A\n" user
    //match user with
    //| Ok user ->
    //    printfn "getting user with id %A" user.Id
    //    let user = CompositionRoot.getUser user.Id |> Async.RunSynchronously
    //    printfn "finished getting user:\n%A" user
    //| Error e -> printfn "Error occured:\n%A" e
    AppConfiguration.configureLog()
    let userId = Guid.Parse "b3f0a6f4-ee04-48ab-b838-9b3330c6bca9"
    let cardNumber = "1234123412341234"
    let createCard =
        { CreateCardCommandModel.CardNumber = cardNumber
          ExpirationMonth = 11us
          ExpirationYear = 2023us
          Name = "Roman Roman"
          UserId = userId }
    let card = CompositionRoot.createCard createCard |> Async.RunSynchronously
    //printfn "card created: [%A]" card
    let user = CompositionRoot.getUser userId |> Async.RunSynchronously
    //printfn "%A" user
    let card = CompositionRoot.getCard cardNumber |> Async.RunSynchronously
    //printfn "%A" card
    //let topUpModel =
    //    { TopUpCommandModel.Number = cardNumber
    //      UserId = userId
    //      TopUpAmount = 10000M}
    //let topUpResult = CompositionRoot.topUp topUpModel |> Async.RunSynchronously
    let setDailyLimitModel =
        { SetDailyLimitCardCommandModel.UserId = userId
          Number = cardNumber
          Limit = 0M}
    let setLimitResult = CompositionRoot.setDailyLimit setDailyLimitModel |> Async.RunSynchronously
    let paymentModel =
        { ProcessPaymentCommandModel.Number = cardNumber
          UserId = userId
          PaymentAmount = 100M}
    let paymentResult = CompositionRoot.processPayment paymentModel |> Async.RunSynchronously
    Console.ReadLine() |> ignore
    0 // return an integer exit code
