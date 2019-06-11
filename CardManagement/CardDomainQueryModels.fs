namespace CardManagement

(*
This module contains mappings of our domain types to something that user/client will see.
Since JSON and a lot of popular languages now do not support Discriminated Unions, which
we heavily use in our domain, we have to convert our domain types to something represented
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
          ExpirationMonth = (fst basicCard.Expiration).ToNumber()
          ExpirationYear = (snd basicCard.Expiration).Value }

    let toCardInfoModel card =
        let (balance, dailyLimit, isActive) =
            match card.AccountDetails with
            | Active accInfo ->
                (accInfo.Balance.Value |> Some, accInfo.DailyLimit.ToDecimalOption(), true)
            | Deactivated -> (None, None, false)
        { BasicInfo = card |> toBasicInfoToModel
          Balance = balance
          DailyLimit = dailyLimit
          IsActive = isActive }

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
        { Id = user.UserInfo.Id
          Name = user.UserInfo.Name.Value
          Address = user.UserInfo.Address |> toAddressModel
          Cards = Set.toList user.Cards |> List.map toCardInfoModel }
