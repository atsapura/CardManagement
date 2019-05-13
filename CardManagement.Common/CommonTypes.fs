namespace CardManagement.Common

[<AutoOpen>]
module CommonTypes =

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
        static member ofNumber field n =
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
            | _ -> validationError field "Number must be from 1 to 12"

    [<Struct>]
    type Year = private Year of uint16
        with
        member this.Value = match this with Year year -> year
        static member create field year =
            if year >= 2019us && year <= 2050us then Year year |> Ok
            else validationError field "Year must be between 2019 and 2050"


    type LetterString = LetterString of string
        with
        member this.Value = match this with LetterString s -> s
        static member create field str =
            match str with
            | (""|null) -> validationError field "string must contain letters"
            | str ->
                if nonLettersRegex.IsMatch(str) then validationError field "string must contain only letters"
                else LetterString str |> Ok

    [<Struct>]
    type Money = Money of decimal
        with
        member this.Value = match this with Money money -> money

    type PostalCode = private PostalCode of string
        with
        member this.Value = match this with PostalCode code -> code
        static member create field str =
            match str with
            | (""|null) -> validationError field "Postal code can't be empty"
            | str ->
                if postalCodeRegex.IsMatch(str)
                    then validationError field "postal code must contain 5 or 6 digits and nothing else"
                else PostalCode str |> Ok
