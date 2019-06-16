namespace CardManagement

module CardWorkflow =
    open CardDomain
    open System
    open CardDomainCommandModels
    open CardManagement.Common
    open CardDomainQueryModels
    open Errors

    type Program<'a> =
        | GetCard of CardNumber * (Card option -> Program<'a>)
        | SaveCard of Card * (unit -> Program<'a>)
        | GetUser of UserId * (User option -> Program<'a>)
        | SaveUser of User * (unit -> Program<'a>)
        | GetAccountInfo of CardNumber * (AccountInfo option -> Program<'a>)
        | SaveAccountInfo of AccountInfo * (unit -> Program<'a>)
        | GetBalanceOperations of (CardNumber * DateTimeOffset * DateTimeOffset) * (BalanceOperation list -> Program<'a>)
        | SaveBalanceOperation of BalanceOperation * (unit -> Program<'a>)
        | Stop of 'a

    let rec bind f instruction =
        match instruction with
        | GetCard (x, next) -> GetCard (x, (next >> bind f))
        | SaveCard (x, next) -> SaveCard (x, (next >> bind f))
        | GetUser (x, next) -> GetUser (x,(next >> bind f))
        | SaveUser (x, next) -> SaveUser (x,(next >> bind f))
        | GetAccountInfo (x, next) -> GetAccountInfo (x,(next >> bind f))
        | SaveAccountInfo (x, next) -> SaveAccountInfo (x,(next >> bind f))
        | GetBalanceOperations (x, next) -> GetBalanceOperations (x,(next >> bind f))
        | SaveBalanceOperation (x, next) -> SaveBalanceOperation (x,(next >> bind f))
        | Stop x -> f x

    let stop x = Stop x
    let getCard number = GetCard (number, stop)
    let saveCard card = SaveCard (card, stop)
    let getUser id = GetUser (id, stop)
    let saveUser user = SaveUser (user, stop)
    let getAccountInfo number = GetAccountInfo (number, stop)
    let saveAccountInfo number = SaveAccountInfo (number, stop)
    let getBalanceOperations (number, fromDate, toDate) = GetBalanceOperations ((number, fromDate, toDate), stop)
    let saveBalanceOperation op = SaveBalanceOperation (op, stop)

    type ProgramBuilder() =
        member __.Bind (x, f) = bind f x
        member this.Bind (x, f) =
            match x with
            | Ok x -> this.ReturnFrom (f x)
            | Error e -> this.Return (Error e)
        member __.Return x = Stop x
        member __.Zero () = Stop ()
        member __.ReturnFrom x = x

    let program = ProgramBuilder()

    let private noneToError (a: 'a option) id =
        let error = EntityNotFound (sprintf "%sEntity" typeof<'a>.Name, id)
        Result.ofOption error a
    
    let processPayment (currentDate: DateTimeOffset) payment =
        program {
            let! cmd = validateProcessPaymentCommand payment |> expectValidationError
            let! card = getCard cmd.CardNumber
            let! card = noneToError card cmd.CardNumber.Value |> expectDataRelatedError
            let today = currentDate.Date |> DateTimeOffset
            let tomorrow = currentDate.Date.AddDays 1. |> DateTimeOffset
            let! operations = getBalanceOperations (cmd.CardNumber, today, tomorrow)
            let spentToday = BalanceOperation.spentAtDate currentDate cmd.CardNumber operations
            let! (card, op) =
                CardActions.processPayment currentDate spentToday card cmd.PaymentAmount
                |> expectOperationNotAllowedError
            do! saveBalanceOperation op
            return card |> toCardInfoModel |> Ok
        }
