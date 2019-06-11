// Learn more about F# at http://fsharp.org

open System

    open CardManagement.CardDomainCommandModels
    open CardManagement.Infrastructure

[<EntryPoint>]
let main argv =
    let createUserCmd =
        { CreateUserCommandModel.Name = "Roman"
          Address =
            { CreateAddressCommandModel.Country = "Russia"
              City = "SaintPetersburg"
              PostalCode = "12345"
              AddressLine1 = "Rubinstein st. 13"
              AddressLine2 = "ap. 1" } }

    printfn "creating user:\n%A\n" createUserCmd
    let user = CompositionRoot.createUser createUserCmd |> Async.RunSynchronously
    printfn "user created:\n%A\n" user
    match user with
    | Ok user ->
        printfn "getting user with id %A" user.Id
        let user = CompositionRoot.getUser user.Id |> Async.RunSynchronously
        printfn "finished getting user:\n%A" user
    | Error e -> printfn "Error occured:\n%A" e
    Console.ReadLine() |> ignore
    0 // return an integer exit code
