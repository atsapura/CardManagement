namespace CardManagement.Data

(*
In our domain types we use types like LetterString, CardNumber etc. with built-in validation.
Those types enforce us to go through validation process, so now we have to validate our
entities during mapping. Normally we shouldn't get any error during this.
We might get it if someone changes data in DB to something invalid directly or if we change
validation rules. In any case we should know about such errors.
*)
module EntityToDomainMapping =
    open CardManagement
    open CardDomain
    open CardDomainEntities
    open Common.Errors
    open FsToolkit.ErrorHandling
    open CardManagement.Common.CommonTypes
    open CardManagement.Common

    // In here validation error means that invalid data was not provided by user, but instead
    // it was in our system. So if we have this error we throw exception
    let private throwOnValidationError entityName (err: ValidationError) =
        sprintf "Could not deserialize entity [%s]. Field [%s]. Message: %s." entityName err.FieldPath err.Message
        |> failwith

    let valueOrException (result: Result< 'a, ValidationError>) : 'a =
        match result with
        | Ok v -> v
        | Error e -> throwOnValidationError typeof<'a>.Name e

    let private validateCardEntityWithAccInfo (cardEntity: CardEntity, cardAccountEntity)
        : Result<Card * AccountInfo, ValidationError> =
        result {
            let! cardNumber = CardNumber.create "cardNumber" cardEntity.CardNumber
            let! name = LetterString.create "name" cardEntity.Name
            let! month = Month.create "expirationMonth" cardEntity.ExpirationMonth
            let! year = Year.create "expirationYear" cardEntity.ExpirationYear
            let accountInfo =
                { Balance = Money cardAccountEntity.Balance
                  DailyLimit = DailyLimit.ofDecimal cardAccountEntity.DailyLimit
                  HolderId = cardEntity.UserId }
            let cardAccountInfo =
                if cardEntity.IsActive then
                    accountInfo
                    |> Active
                else Deactivated
            return
                ({ CardNumber = cardNumber
                   Name = name
                   HolderId = cardEntity.UserId
                   Expiration = (month, year)
                   AccountDetails = cardAccountInfo }, accountInfo)
        }

    let private validateCardEntity (cardEntity: CardEntity, cardAccountEntity) : Result<Card, ValidationError> =
        validateCardEntityWithAccInfo (cardEntity, cardAccountEntity)
        |> Result.map fst

    let mapCardEntity (cardEntity, cardAccountEntity) =
        validateCardEntity (cardEntity, cardAccountEntity)
        |> valueOrException

    let mapCardEntityWithAccountInfo (cardEntity, cardAccountEntity) =
        validateCardEntityWithAccInfo (cardEntity, cardAccountEntity)
        |> valueOrException

    let private validateAddressEntity (entity: AddressEntity) : Result<Address, ValidationError> =
        result {
            let! country = parseCountry entity.Country
            let! city = LetterString.create "city" entity.City
            let! postalCode = PostalCode.create "postalCode" entity.PostalCode
            return
                { Country = country
                  City = city
                  PostalCode = postalCode
                  AddressLine1 = entity.AddressLine1
                  AddressLine2 = entity.AddressLine2 }
        }

    let mapAddressEntity entity = validateAddressEntity entity |> valueOrException

    let private validateUserInfoEntity (entity: UserEntity) : Result<UserInfo, ValidationError> =
        result {
            let! name = LetterString.create "name" entity.Name
            let! address = validateAddressEntity entity.Address
            return
                { Id = entity.UserId
                  Name = name
                  Address = address}
        }

    let mapUserInfoEntity (entity: UserEntity) =
        validateUserInfoEntity entity
        |> valueOrException

    let mapUserEntity (entity: UserEntity) (cardEntities: (CardEntity * CardAccountInfoEntity) list) =
        result {
            let! userInfo = validateUserInfoEntity entity
            let! cards = List.map validateCardEntity cardEntities |> Result.combine

            return
                { UserInfo = userInfo
                  Cards = cards |> Set.ofList }
        } |> valueOrException

    let mapBalanceOperationEntity (entity: BalanceOperationEntity)  =
        result {
            let! cardNumber = entity.Id.CardNumber |> CardNumber.create "id.cardNumber"
            let! balanceChange =
                if entity.BalanceChange < 0M then
                    -entity.BalanceChange |> MoneyTransaction.create |> Result.map Decrease
                else entity.BalanceChange |> MoneyTransaction.create |> Result.map Increase
            return
                { CardNumber = cardNumber
                  NewBalance = Money entity.NewBalance
                  Timestamp = entity.Id.Timestamp
                  BalanceChange = balanceChange }
        } |> valueOrException
