namespace CardManagement


module CardActions =
    open System
    open CardDomain
    open CardManagement.Common.Errors
    open CardManagement.Common

    type CardAccountInfo =
        { UserId: UserId
          Balance: Money
          DailyLimit: DailyLimit }

    let private isExpired (currentDate: DateTimeOffset) (month: Month, year: Year) =
        (int year.Value, month.toNumber() |> int) > (currentDate.Year, currentDate.Month)

    let private setDailyLimitNotAllowed = operationNotAllowed "Set daily limit"

    let private processPaymentNotAllowed = operationNotAllowed "Process payment"

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
            sprintf "Card %s is expired" card.Number.Value
            |> setDailyLimitNotAllowed
        else
        match card with
        | Deactivated _ ->
            sprintf "Card %s is deactivated" card.Number.Value
            |> setDailyLimitNotAllowed
        | Active card -> Active { card with DailyLimit = limit } |> Ok

    let processPayment (currentDate: DateTimeOffset) spentToday card paymentAmount =
        if isCardExpired currentDate card then
            sprintf "Card %s is expired" card.Number.Value
            |> processPaymentNotAllowed
        else
        match card with
        | Deactivated _ ->
            sprintf "Card %s is deactivated" card.Number.Value
            |> processPaymentNotAllowed
        | Active card ->
            if paymentAmount > card.Balance then
                sprintf "Insufficent funds on card %s" card.BasicInfo.Number.Value
                |> processPaymentNotAllowed
            else
            match card.DailyLimit with
            | Limit limit when limit < spentToday + paymentAmount ->
                sprintf "Daily limit is exceeded for card %s" card.BasicInfo.Number.Value
                |> processPaymentNotAllowed
            | _ -> ({ card with Balance = card.Balance - paymentAmount } |> Active, spentToday + paymentAmount)
                   |> Ok

    type ActivateCommand = { CardNumber: CardNumber }

    type DeactivateCommand = { CardNumber: CardNumber }

    type SetDailyLimitCommand =
        { CardNumber: CardNumber
          DailyLimit: DailyLimit }

    type ProcessPaymentCommand =
        { CardNumber: CardNumber
          PaymentAmount: Money }
