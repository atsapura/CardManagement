namespace CardManagement

module CardDomain =

    open System.Text.RegularExpressions
    open CardManagement.Common.Errors
    open CardManagement.Common

    let cardNumberRegex = new Regex("^[0-9]{16}$", RegexOptions.Compiled)

    type CardNumber = private CardNumber of string
        with
        member this.Value = match this with CardNumber s -> s
        static member create str =
            match str with
            | (null|"") -> validationError "card number can't be empty"
            | str ->
                if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
                else validationError "Card number must be of 16 digits only"

    [<Struct>]
    type DailyLimit =
        private
        | Limit of decimal
        | Unlimited
        with
        static member ofDecimal dec =
            if dec > 0m then Limit dec
            else Unlimited

    let (|Limit|Unlimited|) limit =
        match limit with
        | Limit dec -> Limit dec
        | Unlimited -> Unlimited

    type BasicCardInfo =
        { Number: CardNumber
          Name: LetterString
          Expiration: (Month*Year) }

    type UserId = System.Guid

    type CardInfo =
        { BasicInfo: BasicCardInfo
          Balance: Money
          DailyLimit: DailyLimit
          Holder: UserId }

    type Card =
        | Active of CardInfo
        | Deactivated of BasicCardInfo

    type Address =
        { Country: Country
          City: LetterString
          PostalCode: PostalCode 
          AddressLine1: string
          AddressLine2: string }

    type CardDetails =
        { Card: Card 
          HolderAddress: Address
          HolderName: LetterString }

    type User =
        { Name: LetterString
          Id: UserId
          Address: Address
          Cards: Card list }
