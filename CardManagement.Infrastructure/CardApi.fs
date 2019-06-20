namespace CardManagement.Infrastructure

module CardApi =
    open CardManagement
    open Logging

    let createUser arg =
        arg |> ((CardWorkflow.createUser >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.createUser")
    let createCard arg =
        arg |> ((CardWorkflow.createCard >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.createCard")
    let activateCard arg =
        arg |> ((CardWorkflow.activateCard >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.activateCard")
    let deactivateCard arg =
        arg |> ((CardWorkflow.deactivateCard >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.deactivateCard")
    let processPayment arg =
        arg |> ((CardWorkflow.processPayment >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.processPayment")
    let topUp arg =
        arg |> ((CardWorkflow.topUp >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.topUp")
    let setDailyLimit arg =
        arg |> ((CardWorkflow.setDailyLimit >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.setDailyLimit")
    let getCard arg =
        arg |> ((CardWorkflow.getCard >> CardProgramInterpreter.interpret) |> logifyResultAsync "CardApi.getCard")
    let getUser arg =
        arg |> ((CardWorkflow.getUser >> CardProgramInterpreter.interpretSimple) |> logifyResultAsync "CardApi.getUser")
