namespace CardManagement

module CardDomain =
    open System.Text.RegularExpressions
    open CardManagement.Common.Errors

    let cardNumberRegex = new Regex("^[0-9]{16}$", RegexOptions.Compiled)

    let nonLettersRegex = new Regex("\W", RegexOptions.Compiled)

    let postalCodeRegex = new Regex("^[0-9]{5,6}$", RegexOptions.Compiled)

    type Month =
        | January | February | March | April | May | June | July | August | September | October | November | December
        with
        member this.toNumber() =
            match this with
            | January -> 1us
            | February -> 2us
            | March -> 3us
            | April -> 4us
            | May -> 5us
            | June -> 6us
            | July -> 7us
            | August -> 8us
            | September -> 9us
            | October -> 10us
            | November -> 11us
            | December -> 12us
        static member ofNumber n =
            match n with
            | 1us -> January |> Ok
            | 2us -> February |> Ok
            | 3us -> March |> Ok
            | 4us -> April |> Ok
            | 5us -> May |> Ok
            | 6us -> June |> Ok
            | 7us -> July |> Ok
            | 8us -> August |> Ok
            | 9us -> September |> Ok
            | 10us -> October |> Ok
            | 11us -> November |> Ok
            | 12us -> December |> Ok
            | _ -> validationError "Number must be from 1 to 12"

    [<Struct>]
    type Year = private Year of uint16
        with
        member this.Value = match this with Year year -> year
        static member create year =
            if year >= 2019us && year <= 2050us then Year year |> Ok
            else validationError "Year must be between 2019 and 2050"

    type CardNumber = private CardNumber of string
        with
        member this.Value = match this with CardNumber s -> s
        static member create str =
            match str with
            | (null|"") -> validationError "card number can't be empty"
            | str ->
                if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
                else validationError "Card number must be of 16 digits only"

    type LetterString = LetterString of string
        with
        member this.Value = match this with LetterString s -> s
        static member create str =
            match str with
            | (""|null) -> validationError "string must contain letters"
            | str ->
                if nonLettersRegex.IsMatch(str) then validationError ("string must contain only letters")
                else LetterString str |> Ok

    [<Struct>]
    type Money = Money of decimal
        with
        member this.Value = match this with Money money -> money

    type PostalCode = private PostalCode of string
        with
        member this.Value = match this with PostalCode code -> code
        static member create str =
            match str with
            | (""|null) -> validationError "Postal code can't be empty"
            | str ->
                if postalCodeRegex.IsMatch(str)
                    then validationError ("postal code must contain 5 or 6 digits and nothing else")
                else PostalCode str |> Ok

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
