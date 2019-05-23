namespace CardManagement.Data

module DomainToEntityMapping =
    open CardManagement
    open CardDomain
    open CardDomainEntities

    let mapCardToEntity (card: Card) : (CardEntity * CardAccountInfoEntity option) =
        let isActive =
            match card.AccountDetails with
            | Deactivated -> false
            | Active _ -> true
        let details =
            match card.AccountDetails with
            | Deactivated -> None
            | Active details ->
                let limit =
                    match details.DailyLimit with
                    | Unlimited -> 0m
                    | Limit limit -> limit.Value
                { Balance = details.Balance.Value
                  DailyLimit = limit
                  CardNumber = card.Number.Value }
                |> Some
        let card =
            { CardEntity.UserId = card.HolderId
              CardNumber = card.Number.Value
              Name = card.Name.Value
              IsActive = isActive
              ExpirationMonth = (fst card.Expiration).ToNumber()
              ExpirationYear = (snd card.Expiration).Value }
        (card, details)
