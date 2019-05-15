namespace CardManagement

module CardDomainQueryModels =
    open System
    open CardDomain
    open CardManagement.Common

    type AddressModel =
        { Country: string
          City: string
          PostalCode: string
          AddressLine1: string
          AddressLine2: string }

    type BasicCardInfoModel =
        { Number: string
          Name: string
          ExpirationMonth: uint16
          ExpirationYear: uint16 }

    type CardInfoModel =
        { BasicInfo: BasicCardInfoModel
          Balance: decimal option 
          DailyLimit: decimal option
          IsActive: bool }

    type CardDetailsModel =
        { CardInfo: CardInfoModel
          HolderName: string
          HolderAddress: AddressModel }

    type UserModel =
        { Id: Guid
          Name: string
          Address: AddressModel
          Cards: CardInfoModel list }

    let toBasicInfoToModel (basicCard: BasicCardInfo) =
        { Number = basicCard.Number.Value
          Name = basicCard.Name.Value
          ExpirationMonth = (fst basicCard.Expiration).toNumber()
          ExpirationYear = (snd basicCard.Expiration).Value }

    let toCardInfoModel card =
        match card with
        | Active card ->
            { BasicInfo = card.BasicInfo |> toBasicInfoToModel
              Balance = card.Balance.Value |> Some
              DailyLimit =
                match card.DailyLimit with
                | Unlimited -> None
                | Limit limit -> Some limit.Value
              IsActive = true }
        | Deactivated card ->
            { BasicInfo = card |> toBasicInfoToModel
              Balance = None
              DailyLimit = None
              IsActive = false }

    let toAddressModel (address: Address) =
        { Country = address.Country.ToString()
          City = address.City.Value
          PostalCode = address.PostalCode.Value
          AddressLine1 = address.AddressLine1
          AddressLine2 = address.AddressLine2 }

    let toCardDetailsModel (cardDetails: CardDetails) =
        { CardInfo = cardDetails.Card |> toCardInfoModel
          HolderName = cardDetails.HolderName.Value
          HolderAddress = cardDetails.HolderAddress |> toAddressModel }

    let toUserModel (user: User) =
        { Id = user.Id
          Name = user.Name.Value
          Address = user.Address |> toAddressModel
          Cards = List.map toCardInfoModel user.Cards }
