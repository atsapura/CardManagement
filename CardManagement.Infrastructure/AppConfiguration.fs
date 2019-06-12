namespace CardManagement.Infrastructure

module AppConfiguration =
    open Microsoft.Extensions.Configuration
    open System.IO
    open System
    open CardManagement.Data.CardMongoConfiguration
    open Serilog

    let stringSetting defaultValue str = Option.ofObj str |> Option.defaultValue defaultValue

    let intSetting defaultValue (str: string) =
        match Int32.TryParse str with
        | (false, _) -> defaultValue
        | (true, setting) -> setting

    let buildConfig() =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build()

    let configureLog() =
        let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
        Log.Logger <- logger

    let getMongoSettings (config: IConfigurationRoot) =
        let setting = sprintf "MongoDB:%s"
        let database = config.[setting "Database"] |> stringSetting "CardsDb"
        let port = config.[setting "Port"] |> intSetting 27017
        let host = config.[setting "Host"] |> stringSetting "localhost"
        let user = config.[setting "User"] |> stringSetting "root"
        let password = config.[setting "Password"] |> stringSetting "example"
        { Database = database
          Host = host
          Port = port
          User = user
          Password = password }
