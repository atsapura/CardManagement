namespace CardManagement


module CardActions =
    open System
    open CardDomain
    open CardManagement.Common.Errors
    open CardManagement.Common

    let private isExpired (currentDate: DateTimeOffset) (month: Month, year: Year) =
        (int year.Value, month.toNumber() |> int) < (currentDate.Year, currentDate.Month)

    let private setDailyLimitNotAllowed = operationNotAllowed "Set daily limit"

    let private processPaymentNotAllowed = operationNotAllowed "Process payment"

    let private cardExpiredMessage (cardNumber: CardNumber) =
        sprintf "Card %s is expired" cardNumber.Value

    let private cardDeactivatedMessage (cardNumber: CardNumber) =
        sprintf "Card %s is deactivated" cardNumber.Value

    let isCardExpired (currentDate: DateTimeOffset) card =
        isExpired currentDate card.Expiration

    let deactivate card =
        match card.Status with
        | Deactivated -> card
        | Active _ -> { card with Status = Deactivated }

    let activate (cardAccountInfo: AccountInfo) card =
        match card.Status with
        | Active _ -> card
        | Deactivated -> { card with Status = Active cardAccountInfo }

    let setDailyLimit (currentDate: DateTimeOffset) limit card =
        if isCardExpired currentDate card then
            cardExpiredMessage card.Number |> setDailyLimitNotAllowed
        else
        match card.Status with
        | Deactivated -> cardDeactivatedMessage card.Number |> setDailyLimitNotAllowed
        | Active accInfo -> { card with Status = Active { accInfo with DailyLimit = limit } } |> Ok

    let processPayment (currentDate: DateTimeOffset) spentToday card paymentAmount =
        if isCardExpired currentDate card then
            cardExpiredMessage card.Number |> processPaymentNotAllowed
        else
        match card.Status with
        | Deactivated -> cardDeactivatedMessage card.Number |> processPaymentNotAllowed
        | Active accInfo ->
            if paymentAmount > accInfo.Balance then
                sprintf "Insufficent funds on card %s" card.Number.Value
                |> processPaymentNotAllowed
            else
            match accInfo.DailyLimit with
            | Limit limit when limit < spentToday + paymentAmount ->
                sprintf "Daily limit is exceeded for card %s" card.Number.Value
                |> processPaymentNotAllowed
            | Limit _ | Unlimited ->
                ({ card with Status = Active {accInfo with Balance = accInfo.Balance - paymentAmount } }, spentToday + paymentAmount)
                |> Ok

    type ActivateCommand = { CardNumber: CardNumber }

    type DeactivateCommand = { CardNumber: CardNumber }

    type SetDailyLimitCommand =
        { CardNumber: CardNumber
          DailyLimit: DailyLimit }

    type ProcessPaymentCommand =
        { CardNumber: CardNumber
          PaymentAmount: Money }
