namespace Bot
open DSharpPlus.EventArgs
open DSharpPlus
open DSharpPlus.Entities
open Environment

module Commands = 
    exception IncorrectCommandException

    let TryDaily (ctx: MessageCreateEventArgs) (client: DiscordClient) = async {
        try
            if ctx.Message.Content <> Environment.appSettings.Commands.Praise then raise IncorrectCommandException

            let! maybeTimeLeft = Database.GetTicksTillNextDaily()
            match maybeTimeLeft with // if time for daily
            | None ->
                let pray = DiscordEmoji.FromUnicode(appSettings.Display.React) // react to message
                do! ctx.Message.CreateReactionAsync(pray) |> Async.AwaitTask
                do! Database.RecordDaily (ctx.Message.Author.Id |> string) // record
                do! Leaderboard.UpdateLeaderboards client // update leaderboard
            | Some timeLeft ->
                let tillPrayStr = StringUtil.TicksToString timeLeft true true true
                do! ctx.Channel.SendMessageAsync $"{appSettings.Display.NotYetTimeForMessage} Wait {tillPrayStr} more" |> Async.AwaitTask |> Async.Ignore
        with exn -> 
            match exn with
            | IncorrectCommandException -> ()
            | exn -> raise exn
    }

    let HowLong (ctx: MessageCreateEventArgs) (_) = async {
        try
            if ctx.Message.Content <> appSettings.Commands.Howlong then raise IncorrectCommandException

            let! maybeTimeLeft = Database.GetTicksTillNextDaily()
            match maybeTimeLeft with // if there is some time left
            | Some timeLeft ->
                let tillPrayStr = StringUtil.TicksToString timeLeft true true true // gen a pray string
                do! ctx.Channel.SendMessageAsync $"In {tillPrayStr}" |> Async.AwaitTask |> Async.Ignore
            | None ->
                do! ctx.Channel.SendMessageAsync $"It is time" |> Async.AwaitTask |> Async.Ignore
        with exn ->
            match exn with
            | IncorrectCommandException -> ()
            | exn -> raise exn
    }

    let getCommands = [TryDaily; HowLong]