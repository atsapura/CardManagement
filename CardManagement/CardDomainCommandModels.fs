namespace CardManagement

open CardActions
open CardDomain
open CardManagement.Common.Errors
open FsToolkit.ErrorHandling

module CardDomainCommandModels =
    open CardManagement.Common

    type ActivateCardCommandModel =
        { UserId: UserId
          Number: string }

    type DeactivateCardCommandModel =
        { UserId: UserId
          Number: string }

    type SetDailyLimitCardCommandModel =
        { UserId: UserId
          Number: string
          Limit: decimal }

    type ProcessPaymentCommandModel =
        { UserId: UserId
          Number: string
          PaymentAmount: decimal }

    type ValidateActivateCardCommand = ActivateCardCommandModel -> ValidationResult<ActivateCommand>
    type ValidateDeactivateCardCommand = DeactivateCardCommandModel -> ValidationResult<DeactivateCommand>
    type ValidateSetDailyLimitCommand = SetDailyLimitCardCommandModel -> ValidationResult<SetDailyLimitCommand>
    type ValidateProcessPaymentCommand = ProcessPaymentCommandModel -> ValidationResult<ProcessPaymentCommand>

    let private validateCardNumber = CardNumber.create "cardNumber"

    let private validatePaymentAmount amount =
        if amount > 0m then Money amount |> Ok
        else validationError "paymentAmount" "Payment amount must be greater than 0"

    let validateActivateCardCommand : ValidateActivateCardCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> validateCardNumber
                return { ActivateCommand.CardNumber = number }
            }

    let validateDeactivateCardCommand : ValidateDeactivateCardCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> validateCardNumber
                return { CardNumber = number }
            }

    let validateSetDailyLimitCommand : ValidateSetDailyLimitCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> validateCardNumber
                let limit = DailyLimit.ofDecimal cmd.Limit
                return
                    { CardNumber = number
                      DailyLimit = limit }
            }

    let validateProcessPaymentCommand : ValidateProcessPaymentCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> validateCardNumber
                let! amount = cmd.PaymentAmount |> validatePaymentAmount
                return
                    { CardNumber = number
                      PaymentAmount = amount }
            }
