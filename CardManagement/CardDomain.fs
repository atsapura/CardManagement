namespace CardManagement

(*
This file contains our domain types.
There are several important goals to pursue when you do domain modeling:
- Tell AS MUCH as you can with your type: expected states, descriptive naming and so on.
- Make invalid state unrepresentable using private constructors and built in validation.
- Make illegal operations impossible: e.g. if deactivated credit card can't be used for payment,
  hide all the information, which is needed to complete an operation.
*)
module CardDomain =

    open System.Text.RegularExpressions
    open CardManagement.Common.Errors
    open CardManagement.Common
    open System

    let private cardNumberRegex = new Regex("^[0-9]{16}$", RegexOptions.Compiled)

    (*
    Technically card number is represented with a string.
    But it has certain validation rules which we don't want to be violated,
    so instead of throwing exception like one would do in C#, we create separate type,
    make it's constructor private and expose a factory method which returns `Result` with
    possible `ValidationError`. So whenever we have an instance of `CardNumber`, we can be
    certain, that the value inside is valid.
    *)
    type CardNumber = private CardNumber of string
        with
        member this.Value = match this with CardNumber s -> s
        static member create fieldName str =
            match str with
            | (null|"") -> validationError fieldName "card number can't be empty"
            | str ->
                if cardNumberRegex.IsMatch(str) then CardNumber str |> Ok
                else validationError fieldName "Card number must be a 16 digits string"

    (*
    Again, technically daily limit is represented with `decimal`. But `decimal` isn't
    quite what we need here. It can be negative, which is not a valid value for daily limit.
    It can also be a zero, and it may mean that there's no daily limit or it may mean that
    no purchase can be made at all. We could also use `Nullable<decimal>`, but then we would be
    in danger of `NullReferenceException` or someone could along the way use construction `?? 0`.
    In any case this is much easier to read:
    *)
    [<Struct>]
    type DailyLimit =
        private
        | Limit of Money
        | Unlimited
        with
        static member ofDecimal dec =
            if dec > 0m then Money dec |> Limit
            else Unlimited
        member this.ToDecimalOption() =
            match this with
            | Unlimited -> None
            | Limit limit -> Some limit.Value

    (*
    Since we made our constructor private, we can't pattern match it directly from outside,
    so we expose this Active Pattern to be able to see what's inside, but without a possibility
    of direct creation of this type.
    In a nutshell it's sort of `{ get; private set; }` for the whole type.
    *)
    let (|Limit|Unlimited|) limit =
        match limit with
        | Limit dec -> Limit dec
        | Unlimited -> Unlimited

    type UserId = System.Guid

    type AccountInfo =
        { HolderId: UserId
          Balance: Money
          DailyLimit: DailyLimit }
        with
        static member Default userId =
            { HolderId = userId
              Balance = Money 0m
              DailyLimit = Unlimited }

    (*
    This bit is important. As you can see, `AccountInfo` type is holding information about
    the money you have, which is clearly mandatory when you need to process a payment.
    Now, we don't want anyone to be able to process a payment with deactivated card,
    so we just don't provide this information when the card isn't active.
    Now this important business rule can't be violated by accident.
    *)
    type CardAccountInfo =
        | Active of AccountInfo
        | Deactivated

    (*
    We could use `DateTime` type to represent an expiration date. But `DateTime` contains way
    more information then we need. Which would rise a lot of questions:
    - What do we do with the time?
    - What about timezone?
    - What about day of month?
    Now it's clear that expiration is about just month and year.
    *)
    type Card =
        { CardNumber: CardNumber
          Name: LetterString
          HolderId: UserId
          Expiration: (Month * Year)
          AccountDetails: CardAccountInfo }

    type CardDetails =
        { Card: Card 
          HolderAddress: Address
          HolderId: UserId
          HolderName: LetterString }

    type UserInfo =
        { Name: LetterString
          Id: UserId
          Address: Address }

    type User =
        { UserInfo : UserInfo
          Cards: Card list }

    [<Struct>]
    type BalanceChange =
        | Increase of increase: MoneyTransaction
        | Decrease of decrease: MoneyTransaction
        with
        member this.ToDecimal() =
            match this with
            | Increase i -> i.Value
            | Decrease d -> -d.Value

    [<Struct>]
    type BalanceOperation =
        { CardNumber: CardNumber
          Timestamp: DateTimeOffset
          BalanceChange: BalanceChange
          NewBalance: Money }
