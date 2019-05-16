namespace CardManagement

(*
This module contains mappings of our domain types to something that user/client will see.
Since JSON and a lot of popular languages now do not support Discriminated Unions, which
we heavily use in our domain, we have to convert our domain types to something, represented
by common types.
*)
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

    let toBasicInfoToModel (basicCard: Card) =
        { Number = basicCard.Number.Value
          Name = basicCard.Name.Value
          ExpirationMonth = (fst basicCard.Expiration).toNumber()
          ExpirationYear = (snd basicCard.Expiration).Value }

    let toCardInfoModel card =
        match card.AccountDetails with
        | Active accInfo ->
            { BasicInfo = card |> toBasicInfoToModel
              Balance = accInfo.Balance.Value |> Some
              DailyLimit =
                match accInfo.DailyLimit with
                | Unlimited -> None
                | Limit limit -> Some limit.Value
              IsActive = true }
        | Deactivated ->
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
