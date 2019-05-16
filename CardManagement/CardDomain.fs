namespace CardManagement

module CardDomain =

    open System.Text.RegularExpressions
    open CardManagement.Common.Errors
    open CardManagement.Common

    let cardNumberRegex = new Regex("^[0-9]{16}$", RegexOptions.Compiled)

    type CardNumber = private CardNumber of string
        with
        member this.Value = match this with CardNumber s -> s
        static member create fieldName str =
            match str with
            | (null|"") -> validationError fieldName "card number can't be empty"
            | str ->
                if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
                else validationError fieldName "Card number must be of 16 digits only"

    [<Struct>]
    type DailyLimit =
        private
        | Limit of Money
        | Unlimited
        with
        static member ofDecimal dec =
            if dec > 0m then Money dec |> Limit
            else Unlimited

    let (|Limit|Unlimited|) limit =
        match limit with
        | Limit dec -> Limit dec
        | Unlimited -> Unlimited

    type UserId = System.Guid

    type AccountInfo =
        { Holder: UserId
          Balance: Money
          DailyLimit: DailyLimit }

    type CardStatus =
        | Active of AccountInfo
        | Deactivated

    type Card =
        { Number: CardNumber
          Name: LetterString
          Expiration: (Month * Year)
          Status: CardStatus }

    type CardDetails =
        { Card: Card 
          HolderAddress: Address
          HolderName: LetterString }

    type User =
        { Name: LetterString
          Id: UserId
          Address: Address
          Cards: Card list }
