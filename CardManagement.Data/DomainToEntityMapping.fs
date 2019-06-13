namespace CardManagement.Data

module DomainToEntityMapping =
    open CardManagement
    open CardDomain
    open CardDomainEntities
    open CardManagement.Common

    type MapCardAccountInfo = CardNumber * AccountInfo -> CardAccountInfoEntity
    type MapCard = Card -> CardEntity * CardAccountInfoEntity option
    type MapAddress = Address -> AddressEntity
    type MapUser = UserInfo -> UserEntity
    type MapBalanceOperation = BalanceOperation -> BalanceOperationEntity

    let mapAccountInfoToEntity : MapCardAccountInfo =
        fun (cardNumber, accountInfo) ->
        let limit =
            match accountInfo.DailyLimit with
            | Unlimited -> 0m
            | Limit limit -> limit.Value
        { Balance = accountInfo.Balance.Value
          DailyLimit = limit
          CardNumber = cardNumber.Value }

    let mapCardToEntity : MapCard =
        fun card ->
        let isActive =
            match card.AccountDetails with
            | Deactivated -> false
            | Active _ -> true
        let details =
            match card.AccountDetails with
            | Deactivated -> None
            | Active accountInfo ->
                mapAccountInfoToEntity (card.Number, accountInfo)
                |> Some
        let card =
            { CardEntity.UserId = card.HolderId
              CardNumber = card.Number.Value
              Name = card.Name.Value
              IsActive = isActive
              ExpirationMonth = (fst card.Expiration).ToNumber()
              ExpirationYear = (snd card.Expiration).Value }
        (card, details)

    let mapAddressToEntity : MapAddress =
        fun address ->
        { AddressEntity.Country = address.Country.ToString()
          City = address.City.Value
          PostalCode = address.PostalCode.Value
          AddressLine1 = address.AddressLine1
          AddressLine2 = address.AddressLine2 }

    let mapUserToEntity : MapUser =
        fun user ->
        { UserId = user.Id
          Address = user.Address |> mapAddressToEntity
          Name = user.Name.Value }

    let mapBalanceOperationToEntity : MapBalanceOperation =
        fun operation ->
        { Id = { Timestamp = operation.Timestamp; CardNumber = operation.CardNumber.Value}
          NewBalance = operation.NewBalance.Value
          BalanceChange =
            match operation.BalanceChange with
            | Increase v -> v.Value
            | Decrease v -> -v.Value }
