namespace CardManagement

open CardDomain
open CardManagement.Common.Errors

module CardActions =
    open System

    type CardAccountInfo =
        { UserId: UserId
          Balance: Money
          DailyLimit: DailyLimit }

    let private isExpired (currentDate: DateTimeOffset) (month: Month, year: Year) =
        (int year.Value, month.toNumber() |> int) > (currentDate.Year, currentDate.Month)

    let isCardExpired (currentDate: DateTimeOffset) card =
        let isExpired = isExpired currentDate
        match card with
        | Deactivated card -> isExpired card.Expiration
        | Active card -> isExpired card.BasicInfo.Expiration

    let deactivate card =
        match card with
        | Deactivated _ -> card
        | Active card -> card.BasicInfo |> Deactivated

    let activate (cardAccountInfo: CardAccountInfo) card =
        match card with
        | Active _ -> card
        | Deactivated bacisInfo ->
            { BasicInfo = bacisInfo
              DailyLimit = cardAccountInfo.DailyLimit
              Balance = cardAccountInfo.Balance
              Holder = cardAccountInfo.UserId }
            |> Active

    let setDailyLimit (currentDate: DateTimeOffset) limit card =
        if isCardExpired currentDate card then
            operationNotAllowed "Card is expired"
        else
        match card with
        | Deactivated _ -> operationNotAllowed "Card deactivated"
        | Active card -> Active { card with DailyLimit = limit } |> Ok

    type ActivateCommand = { CardNumber: CardNumber }

    type DeactivateCommand = { CardNumber: CardNumber }

    type SetDailyLimitCommand =
        { CardNumber: CardNumber
          DailyLimit: DailyLimit }
