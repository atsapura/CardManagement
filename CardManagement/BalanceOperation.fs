namespace CardManagement

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BalanceOperation =
    open CardDomain
    open System
    open CardManagement.Common

    let isDecrease change =
        match change with
        | Increase _ -> false
        | Decrease _ -> true

    let spentAtDate (date: DateTimeOffset) cardNumber operations =
        let date = date.Date
        let operationFilter { CardNumber = number; BalanceChange = change; Timestamp = timestamp } =
            isDecrease change && number = cardNumber && timestamp.Date = date
        let spendings = List.filter operationFilter operations
        List.sumBy (fun s -> s.BalanceChange.ToDecimal()) spendings |> Money
