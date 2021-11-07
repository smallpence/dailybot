namespace Bot
open System.Threading.Tasks
open Microsoft.FSharp.Control
open DSharpPlus
open Emzi0767.Utilities
open DSharpPlus.EventArgs
open System
open DSharpPlus.Entities

module Main =
    exception MyMessageException

    let run = async {
        try
            let dconf = DiscordConfiguration ( Token = Environment.appSecrets.Bottoken, TokenType = TokenType.Bot, Intents = DiscordIntents.AllUnprivileged )
            let discord = new DiscordClient (dconf)
            
            // ready stuff
            discord.add_Ready <| new AsyncEventHandler<DiscordClient, ReadyEventArgs> (fun client _ -> 
                async {
                    // display current thoughts of next ticks
                    let! maybeTimeLeft = Database.GetTicksTillNextDaily()
                    printfn "Next daily ticks retrieved as %A" maybeTimeLeft
                    if maybeTimeLeft.IsSome then printfn $"aka {StringUtil.TicksToString maybeTimeLeft.Value false true true}"
                    printfn "Logged in!"

                    // display leaderboard upon login
                    do! Leaderboard.UpdateLeaderboards client

                    do! Async.Ignore <| Async.StartChild (async {
                        // every minute update status
                        while true do
                            let! maybeTimeLeft = Database.GetTicksTillNextDaily()
                            let status = 
                                match maybeTimeLeft with
                                | Some timeLeft -> StringUtil.TicksToString timeLeft false true true
                                | None -> Bot.Environment.appSettings.Display.TimeForMessage // if no time left
                            do! client.UpdateStatusAsync(DiscordActivity(status)) |> Async.AwaitTask
                            do! Async.Sleep (1000 * 60)
                    }) 

                } |> Async.StartAsTask :> Task
            )

            discord.add_MessageCreated <| new AsyncEventHandler<DiscordClient, MessageCreateEventArgs> (fun client args ->
                async {
                    try
                        // ignore my messages (but view other bots')
                        if args.Author.Id = client.CurrentUser.Id then raise MyMessageException
                        
                        return! Commands.getCommands
                        |> List.map (fun f -> f args client) // apply context to each command
                        |> Async.Sequential // try each command in turn
                        |> Async.Ignore
                    with error -> 
                        match error with
                        | MyMessageException -> ()
                        | exn -> raise exn
                } |> Async.StartAsTask :> Task
            )

            do! discord.ConnectAsync() |> Async.AwaitTask
            do! Task.Delay -1 |> Async.AwaitTask

        with exn -> 
            do! Logging.recordExn exn
            // raise exn
    }
