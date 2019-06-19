namespace CardManagement

module CardProgramBuilder =
    open CardDomain
    open System
    open CardManagement.Common
    open Errors

    (*
    Ok, this requires some explanation.
    So we've created a lot of functions for validation, logic and model mapping.
    All of those functions are pure, so combining them in here is totally fine.
    But we have to interact with DB and functions for that are in another layer, we don't have access to them.
    However we have to make business logic decisions based on output of those functions,
    so we have to emulate or inject them or whatever.
    In OOP they use DI frameworks for that, but since the ultimate goal is to move as many errors
    in compile time as possible, using classic IoC container would be a step in the opposite direction.
    How do we solve this? At first I chose the most obvious way and just passed all the dependencies
    in the functions. I kept this code in `obsolete-dependency-managing` branch, see `CardPipeline.fs` file.
    Another option (this one) is to use Interpreter pattern.
    The idea is that we divide our composition code in 2 parts: execution tree and interpreter for that tree.
    Execution tree is a set of sequentual instructions, like this:
    - validate input card number, if it's valid
    - get me a card by that number. If there's one
    - activate it.
    - save result.
    - map it to model and return.
    Now, this tree doesn't know what database we use, what library we use to call it,
    it doesn't even know whether we use sync or async calls to do that.
    All it knows is a name of operation, input parameter type and return type.
    Basically a signature, but without any side effect information, e.g. `Card` instead of `Task<Card>` or `Async<Card>`.
    But since we are building a tree structure, instead of using interfaces or plain function signatures,
    we use union type with a tuple inside every case.
    We use 1 union for 1 bounded context (in our case the whole app is 1 context).
    This union represents all the possible dependencies we use in this bounded context.
    Every case replresent a placeholder for a dependency.
    First element of a tuple inside the case is an input parameter of dependency.
    A second tuple is a function, which receives an output parameter of that dependency
    and returns the rest of our execution tree branch.
    *)
    type Program<'a> =
        | GetCard of CardNumber * (Card option -> Program<'a>)
        | GetCardWithAccountInfo of CardNumber * ((Card*AccountInfo) option -> Program<'a>)
        | CreateCard of (Card*AccountInfo) * (Result<unit, DataRelatedError> -> Program<'a>)
        | ReplaceCard of Card * (Result<unit, DataRelatedError> -> Program<'a>)
        | GetUser of UserId * (User option -> Program<'a>)
        | CreateUser of UserInfo * (Result<unit, DataRelatedError> -> Program<'a>)
        | GetBalanceOperations of (CardNumber * DateTimeOffset * DateTimeOffset) * (BalanceOperation list -> Program<'a>)
        | SaveBalanceOperation of BalanceOperation * (Result<unit, DataRelatedError> -> Program<'a>)
        | Stop of 'a

    // This bind function allows you to pass a continuation for current node of your expression tree
    // the code is basically a boiler plate, as you can see.
    let rec bind f instruction =
        match instruction with
        | GetCard (x, next) -> GetCard (x, (next >> bind f))
        | GetCardWithAccountInfo (x, next) -> GetCardWithAccountInfo (x, (next >> bind f))
        | CreateCard (x, next) -> CreateCard (x, (next >> bind f))
        | ReplaceCard (x, next) -> ReplaceCard (x, (next >> bind f))
        | GetUser (x, next) -> GetUser (x,(next >> bind f))
        | CreateUser (x, next) -> CreateUser (x,(next >> bind f))
        | GetBalanceOperations (x, next) -> GetBalanceOperations (x,(next >> bind f))
        | SaveBalanceOperation (x, next) -> SaveBalanceOperation (x,(next >> bind f))
        | Stop x -> f x


    // this is a set of basic functions. Use them in your expression tree builder to represent dependency call
    let stop x = Stop x
    let getCardByNumber number = GetCard (number, stop)
    let getCardWithAccountInfo number = GetCardWithAccountInfo (number, stop)
    let createNewCard (card, acc) = CreateCard ((card, acc), stop)
    let replaceCard card = ReplaceCard (card, stop)
    let getUserById id = GetUser (id, stop)
    let createNewUser user = CreateUser (user, stop)
    let getBalanceOperations (number, fromDate, toDate) = GetBalanceOperations ((number, fromDate, toDate), stop)
    let saveBalanceOperation op = SaveBalanceOperation (op, stop)

    // These are builders for computation expressions. Using CEs will make building execution trees very easy
    type SimpleProgramBuilder() =
        member __.Bind (x, f) = bind f x
        member __.Return x = Stop x
        member __.Zero () = Stop ()
        member __.ReturnFrom x = x

    type ProgramBuilder() =
        member __.Bind (x, f) = bind f x
        member this.Bind (x, f) =
            match x with
            | Ok x -> this.ReturnFrom (f x)
            | Error e -> this.Return (Error e)
        member this.Bind((x: Program<Result<_,_>>), f) =
            let f x =
                match x with
                | Ok x -> this.ReturnFrom (f x)
                | Error e -> this.Return (Error e )
            this.Bind(x, f)
        member __.Return x = Stop x
        member __.Zero () = Stop ()
        member __.ReturnFrom x = x

    let program = ProgramBuilder()
    let simpleProgram = SimpleProgramBuilder()

    // This is example of using a computation expression `program` from above
    let expectDataRelatedErrorProgram (prog: Program<Result<_,DataRelatedError>>) =
        program {
            let! result = prog //here we retrieve return value from our program `prog`. Like async/await in C#
            return expectDataRelatedError result
        }
