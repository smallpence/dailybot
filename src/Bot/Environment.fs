namespace Bot

open FSharp.Data
open System.IO

module Environment =
    type AppSettingsProvider = JsonProvider<"../../appsettings.json">
    let appSettings = AppSettingsProvider.Parse(File.ReadAllText("appsettings.json"))

    type AppSecretsProvider = JsonProvider<"../../appsecrets.json">
    let appSecrets = AppSecretsProvider.Parse(File.ReadAllText("appsecrets.json"))
