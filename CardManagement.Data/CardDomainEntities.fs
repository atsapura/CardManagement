namespace CardManagement.Data

module CardDomainEntities =
    open System
    open CardManagement.Common
    open MongoDB.Bson.Serialization.Attributes

    type UserId = Guid

    [<CLIMutable>]
    type AddressEntity =
        { Id: Guid
          Country: string
          City: string
          PostalCode: string
          AddressLine1: string
          AddressLine2: string }

    [<CLIMutable>]
    type CardEntity =
        { [<BsonId>]
          CardNumber: string
          Name: string
          IsActive: bool
          ExpirationMonth: uint16
          ExpirationYear: uint16
          UserId: UserId }

    [<CLIMutable>]
    type CardAccountInfoEntity =
        { [<BsonId>]
          CardNumber: string
          Balance: decimal
          DailyLimit: decimal }

    [<CLIMutable>]
    type UserEntity =
        { [<BsonId>]
          UserId: UserId
          Name: string
          Address: AddressEntity }

    let isNullUnsafe (arg: 'a when 'a: not struct) =
        arg = Unchecked.defaultof<'a>
