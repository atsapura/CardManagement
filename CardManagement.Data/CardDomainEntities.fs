namespace CardManagement.Data

module CardDomainEntities =
    open System
    open CardManagement.Common
    open MongoDB.Bson.Serialization.Attributes
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq.RuntimeHelpers

    type UserId = Guid

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

    let isNullUnsafe (arg: 'a when 'a: not struct) =
        arg = Unchecked.defaultof<'a>

    let unsafeNullToOption a =
        if isNullUnsafe a then None else Some a

    let inline (|HasEntityId|) x =
        fun () -> (^a : (member EntityId: string) x)

    let inline entityId (HasEntityId f) = f()

    let inline (|HasIdComparer|) x =
        fun () -> (^a : (member IdComparer: Quotations.Expr<Func< ^a, bool>>) x)

    let inline idComparer (HasIdComparer id) =
        id()
        |> LeafExpressionConverter.QuotationToExpression 
        |> unbox<Expression<Func<_,_>>>
