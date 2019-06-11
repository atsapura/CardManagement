﻿namespace CardManagement

(*
This module contains command models, validated commands and validation functions.
In C# common pattern is to throw exception if input is invalid and pass it further if it's ok.
Problem with that approach is if we forget to validate, the code will compile and a program
either won't crash at all, or it will in some unexpected place. So we have to cover that with unit tests.
Here however we use different types for validated entities.
So even if we try to miss validation, the code won't even compile.
*)
module CardDomainCommandModels =
    open CardManagement.Common
    open CardDomain
    open CardManagement.Common.Errors
    open FsToolkit.ErrorHandling

    type ActivateCommand = { CardNumber: CardNumber }

    type DeactivateCommand = { CardNumber: CardNumber }

    type SetDailyLimitCommand =
        { CardNumber: CardNumber
          DailyLimit: DailyLimit }

    type ProcessPaymentCommand =
        { CardNumber: CardNumber
          PaymentAmount: Money }

    [<CLIMutable>]
    type ActivateCardCommandModel =
        { UserId: UserId
          Number: string }

    [<CLIMutable>]
    type DeactivateCardCommandModel =
        { UserId: UserId
          Number: string }

    [<CLIMutable>]
    type SetDailyLimitCardCommandModel =
        { UserId: UserId
          Number: string
          Limit: decimal }

    [<CLIMutable>]
    type ProcessPaymentCommandModel =
        { UserId: UserId
          Number: string
          PaymentAmount: decimal }

    [<CLIMutable>]
    type CreateAddressCommandModel =
        { Country: string
          City: string
          PostalCode: string
          AddressLine1: string
          AddressLine2: string }

    [<CLIMutable>]
    type CreateUserCommandModel =
        { Name: string
          Address: CreateAddressCommandModel }

    [<CLIMutable>]
    type CreateCardCommandModel =
        { CardNumber : string
          Name: string
          ExpirationMonth: uint16
          ExpirationYear: uint16
          UserId: UserId }

    (*
    This is a brief API description made with just type aliases.
    As you can see, every public function here returns a `Result` with possible `ValidationError`.
    No other error can occur in here.
    *)
    type ValidateActivateCardCommand   = ActivateCardCommandModel      -> ValidationResult<ActivateCommand>
    type ValidateDeactivateCardCommand = DeactivateCardCommandModel    -> ValidationResult<DeactivateCommand>
    type ValidateSetDailyLimitCommand  = SetDailyLimitCardCommandModel -> ValidationResult<SetDailyLimitCommand>
    type ValidateProcessPaymentCommand = ProcessPaymentCommandModel    -> ValidationResult<ProcessPaymentCommand>
    type ValidateCreateAddressCommand  = CreateAddressCommandModel     -> ValidationResult<Address>
    type ValidateCreateUserCommand     = CreateUserCommandModel        -> ValidationResult<UserInfo>
    type ValidateCreateCardCommand     = CreateCardCommandModel        -> ValidationResult<Card>

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

    let validateCreateAddressCommand : ValidateCreateAddressCommand =
        fun cmd ->
        result {
            let! country = parseCountry cmd.Country
            let! city = LetterString.create "city" cmd.City
            let! postalCode = PostalCode.create "postalCode" cmd.PostalCode
            return
                { Address.Country = country
                  City = city
                  PostalCode = postalCode
                  AddressLine1 = cmd.AddressLine1
                  AddressLine2 = cmd.AddressLine2}
        }

    let validateCreateUserCommand userId : ValidateCreateUserCommand =
        fun cmd ->
        result {
            let! name = LetterString.create "name" cmd.Name
            let! address = validateCreateAddressCommand cmd.Address
            return
                { UserInfo.Id = userId
                  Name = name
                  Address = address }
        }

    let validateCreateCardCommand : ValidateCreateCardCommand =
        fun cmd ->
        result {
            let! name = LetterString.create "name" cmd.Name
            let! number = CardNumber.create "cardNumber" cmd.CardNumber
            let! month = Month.create "expirationMonth" cmd.ExpirationMonth
            let! year = Year.create "expirationYear" cmd.ExpirationYear
            return
                { Card.Number = number
                  Name = name
                  HolderId = cmd.UserId
                  Expiration = month,year
                  AccountDetails =
                     AccountInfo.Default cmd.UserId
                     |> Active }
        }
