namespace CardManagement.Data

module EntityToDomainMapping =
    open CardManagement
    open CardDomain
    open CardDomainEntities
    open Common.Errors
    open FsToolkit.ErrorHandling
    open CardManagement.Common.CommonTypes
    open CardManagement.Common

    let private mapValidationError entityName entityId (err: ValidationError) =
        { EntityId = entityId
          EntityName = entityName
          Message = sprintf "Could not deserialize field %s. Message: %s" err.FieldPath err.Message }

    let private validateCardEntity (cardEntity: CardEntity, cardAccountEntity) : Result<Card, ValidationError> =
        result {
            let! cardNumber = CardNumber.create "cardNumber" cardEntity.CardNumber
            let! name = LetterString.create "name" cardEntity.Name
            let! month = Month.ofNumber "expirationMonth" cardEntity.ExpirationMonth
            let! year = Year.create "expirationYear" cardEntity.ExpirationYear
            let accountInfo =
                if cardEntity.IsActive then
                    { Balance = Money cardAccountEntity.Balance
                      DailyLimit = DailyLimit.ofDecimal cardAccountEntity.DailyLimit
                      Holder = cardEntity.UserId }
                    |> Active
                else Deactivated
            return
                { Number = cardNumber
                  Name = name
                  HolderId = cardEntity.UserId
                  Expiration = (month, year)
                  AccountDetails = accountInfo }
        }

    let mapCardEntity (cardEntity, cardAccountEntity) =
        validateCardEntity (cardEntity, cardAccountEntity)
        |> Result.mapError (mapValidationError "card" cardEntity.CardNumber)

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

    let mapAddressEntity entity : Result<Address, InvalidDbDataError>=
        validateAddressEntity entity
        |> Result.mapError (mapValidationError "address" (entity |> sprintf "%A"))

    let private validateUserInfoEntity (entity: UserEntity) : Result<UserInfo, ValidationError> =
        result {            
            let! name = LetterString.create "name" entity.Name
            let! address = validateAddressEntity entity.Address
            return
                { Id = entity.UserId
                  Name = name
                  Address = address}
        }

    let mapUserInfoEntity (entity: UserEntity) : Result<UserInfo, InvalidDbDataError> =
        validateUserInfoEntity entity
        |> Result.mapError (mapValidationError "user" <| entityId entity)

    let mapUserEntity (entity: UserEntity) (cardEntities: (CardEntity * CardAccountInfoEntity) list)
        : Result<User, InvalidDbDataError> =
        result {
            let! userInfo = validateUserInfoEntity entity
            let! cards = List.map validateCardEntity cardEntities |> Result.combine

            return
                { UserInfo = userInfo
                  Cards = cards |> Set.ofList }
        } |> Result.mapError (mapValidationError "user" <| entityId entity)
