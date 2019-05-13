namespace CardManagement

open CardActions
open CardDomain
open CardManagement.Common.Errors
open FsToolkit.ErrorHandling

module CardDomainCommandModels =

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

    type ValidateActivateCardCommand = ActivateCardCommandModel -> Result<ActivateCommand, ValidationError>
    type ValidateDeactivateCardCommand = DeactivateCardCommandModel -> Result<DeactivateCommand, ValidationError>
    type ValidateSetDailyLimitCommand = SetDailyLimitCardCommandModel -> Result<SetDailyLimitCommand, ValidationError>

    let validateActivateCardCommand : ValidateActivateCardCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> CardNumber.create "cardNumber"
                return { ActivateCommand.CardNumber = number }
            }

    let validateDeactivateCardCommand : ValidateDeactivateCardCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> CardNumber.create "cardNumber"
                return { CardNumber = number }
            }

    let validateSetDailyLimitCommand : ValidateSetDailyLimitCommand =
        fun cmd ->
            result {
                let! number = cmd.Number |> CardNumber.create "cardNumber"
                let limit = DailyLimit.ofDecimal cmd.Limit
                return
                    { CardNumber = number
                      DailyLimit = limit }
            }
