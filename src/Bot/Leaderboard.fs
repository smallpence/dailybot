namespace Bot

open Database
open DSharpPlus
open DSharpPlus.Entities
open Environment

module Leaderboard = 
    let StringifyLeaderboard (client:DiscordClient) leaderboardData = async {
        // turn an entry into a pairing of discorduser (or if if user cant be found) and their score
        let parseUser (x:LeaderboardEntry) = async {
            try 
                let! user = client.GetUserAsync(uint64(x._id)) |> Async.AwaitTask;
                return (Choice1Of2 user, x.Total)
            with // return none if this cant be found
            | _ -> return (Choice2Of2 x._id, x.Total)
        }

        let stringifyUser (choice: Choice<DiscordUser,string>) =
            match choice with
            | Choice1Of2 user -> user.Username
            | Choice2Of2 id -> id
        
        // get a leaderboard with fetched discord user
        let! userLeaderboard = 
            leaderboardData
            |> Seq.truncate 10
            |> List.ofSeq
            |> List.map parseUser
            |> Async.Parallel // get all users in parallel

        // turn the users into strings
        let stringLeaderboard = 
            userLeaderboard
            |> Array.map (fun (user, total) -> $"{stringifyUser user} - {total}")
            |> String.concat "\n"

        return stringLeaderboard
    }

    let BuildLeaderboard (client:DiscordClient) = async {
        let stringify = StringifyLeaderboard client

        // lots of async to do
        let! alltimeLeaderboard = GetAlltimeLeaderboardData
        let! monthlyLeaderboard = GetMonthlyLeaderboardData
        let! alltimeLeaderboard = stringify alltimeLeaderboard
        let! monthlyLeaderboard = stringify monthlyLeaderboard

        let embed = // how weird seeing builder notation in F#
            DiscordEmbedBuilder()
                .WithTitle("Leaderboard")
                .AddField("All time",$"```{alltimeLeaderboard}```",true)
                .AddField("Monthly",$"```{monthlyLeaderboard}```",true)
                .Build()

        return embed
    }

    let GetLeaderboardChannels (client: DiscordClient) = async {
        let GetLeaderboardChannel (guild: DiscordGuild) = async {
            let! channels = guild.GetChannelsAsync() |> Async.AwaitTask

            let leaderboardChannels = 
                channels
                |> Seq.filter (fun channel -> channel.Name = appSettings.Channels.Leaderboard)

            // return the first element, or none if seq is empty (no leaderboard channel)
            return Seq.tryFind (fun _ -> true) leaderboardChannels
        }

        let! leaderboardChannels = 
            client.Guilds.Values
            |> Seq.map GetLeaderboardChannel
            |> Async.Parallel
        let leaderboardChannels = Array.choose id leaderboardChannels

        return List.ofArray leaderboardChannels
    }

    let TryFetchLastMessage (channel: DiscordChannel) id = async {
        try 
            if channel.LastMessageId.HasValue then
                let lastId = channel.LastMessageId.Value
                let! lastMessage = channel.GetMessageAsync(lastId) |> Async.AwaitTask
                // dont try and edit a message that isnt mine
                return if lastMessage.Author.Id = id then Some lastMessage else None
            else return None
        with // if message couldnt be accessed
        | e -> return None
    }

    let UpdateLeaderboard (client: DiscordClient) (embed: DiscordEmbed) (channel: DiscordChannel) = async {
        try
            match! TryFetchLastMessage channel client.CurrentUser.Id with
            | Some message -> do! message.ModifyAsync(Optional.FromValue embed) |> Async.AwaitTask |> Async.Ignore
            | None -> do! channel.SendMessageAsync(embed) |> Async.AwaitTask |> Async.Ignore
        with
        | e -> ()
    }

    let UpdateLeaderboards client = async {
        let! pietyChannels = GetLeaderboardChannels client
        let! embed = BuildLeaderboard client
        
        do! 
            pietyChannels
            |> List.map (UpdateLeaderboard client embed) // modify update in this channel
            |> Async.Parallel // process all channels in parallel
            |> Async.Ignore // ignore results (which is a null array)
    }