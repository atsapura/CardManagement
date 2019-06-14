namespace CardManagement.Data

module CardDomainEntities =
    open System
    open MongoDB.Bson.Serialization.Attributes
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq.RuntimeHelpers

    type UserId = Guid

    (*
    Over here we have entities for storing our stuff to DB.
    We use simple structures so they can be represented via JSON.
    Every entity has a different identifier, for User it's Guid `UserId` where for the card it's card number itself.
    However we still need some standard way for error messages, e.g. when we want to inform user when
    entity with specified Id wasn't found. So we use string `EntityId` property for representing that.
    *)

    [<CLIMutable>]
    type AddressEntity =
        { Country: string
          City: string
          PostalCode: string
          AddressLine1: string
          AddressLine2: string }
        with
        member this.EntityId = sprintf "%A" this

    [<CLIMutable>]
    type CardEntity =
        { [<BsonId>]
          CardNumber: string
          Name: string
          IsActive: bool
          ExpirationMonth: uint16
          ExpirationYear: uint16
          UserId: UserId }
        with
        member this.EntityId = this.CardNumber.ToString()
        // we use this Id comparer quotation (F# alternative to C# Expression) for updating entity by id,
        // since for different entities identifier has different name and type
        member this.IdComparer = <@ System.Func<_,_> (fun c -> c.CardNumber = this.CardNumber) @>

    [<CLIMutable>]
    type CardAccountInfoEntity =
        { [<BsonId>]
          CardNumber: string
          Balance: decimal
          DailyLimit: decimal }
        with
        member this.EntityId = this.CardNumber.ToString()
        member this.IdComparer = <@ System.Func<_,_> (fun c -> c.CardNumber = this.CardNumber) @>

    [<CLIMutable>]
    type UserEntity =
        { [<BsonId>]
          UserId: UserId
          Name: string
          Address: AddressEntity }
        with
        member this.EntityId = this.UserId.ToString()
        member this.IdComparer = <@ System.Func<_,_> (fun c -> c.UserId = this.UserId) @>

    // MongoDb allowes you to use objects as identifiers, so I used this instead of generating some GUID
    // which wouldn't mean anything other than something purely DB specific
    [<CLIMutable>]
    type BalanceOperationId =
        { Timestamp: DateTimeOffset
          CardNumber: string }

    [<CLIMutable>]
    type BalanceOperationEntity =
        { [<BsonId>]
          Id: BalanceOperationId
          BalanceChange: decimal
          NewBalance: decimal }
        with
        member this.EntityId = sprintf "%A" this.Id

    (*
    Now here's a little trick: by default F# doesn't allow to use nulls for records and discriminated unions.
    So you can't even use construct `if myRecord = null then ...`. Therefore your F# code is null safe.
    However we are living in .NET and right now we are using C# library to interact with MongoDB.
    This library will return nulls when there's nothing in DB, so we use this `Unchecked.defaultof<>`,
    which for reference types returns null.
    *)
    let isNullUnsafe (arg: 'a when 'a: not struct) =
        arg = Unchecked.defaultof<'a>

    // then we have this function to convert nulls to option, therefore we limited this
    // toxic null thing in here.
    let unsafeNullToOption a =
        if isNullUnsafe a then None else Some a

    (*
    Here's another cool feature of F#: we can do structural typing.
    Every entity now has `EntityId`, but instead of creating some dull interface and implementing it
    in every entity, we can define this 2 functions for retrieving `string EntityId` from every type
    that has it.
    Note that it works in COMPILE time, it's not reflection magic or something.
    For more examples of this thing take a look at https://gist.github.com/atsapura/fd9d7aa26e337eaa2f7f04d6cbb58ef6
    *)
    let inline (|HasEntityId|) x =
        fun () -> (^a : (member EntityId: string) x)

    let inline entityId (HasEntityId f) = f()

    let inline (|HasIdComparer|) x =
        fun () -> (^a : (member IdComparer: Quotations.Expr<Func< ^a, bool>>) x)

    // We need to convert F# quotations to C# expressions which C# mongo db driver understands.
    let inline idComparer (HasIdComparer id) =
        id()
        |> LeafExpressionConverter.QuotationToExpression 
        |> unbox<Expression<Func<_,_>>>
