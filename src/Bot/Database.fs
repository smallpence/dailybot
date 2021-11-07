namespace Bot

open MongoDB.Bson
open MongoDB.Driver
open System
open Bot.Environment

module Database =
    let mutable nextDailyTicks: option<int64> = None

    // id = ticks of prayer
    type Daily = { _id: BsonTimestamp; UnixTicks: int64; DiscordID: string }
    // id = discord uuid
    type LeaderboardEntry = { _id: string; Total: int }

    // connect to db and init variables
    let connectionString = appSecrets.Databasekey
    let client = MongoClient connectionString
    let db = client.GetDatabase(appSettings.Mongodb.Database)
    let dailyCollection = db.GetCollection<Daily> appSettings.Mongodb.Collections.Main
    let alltimeLeaderboardCollection = db.GetCollection<LeaderboardEntry> appSettings.Mongodb.Collections.AlltimeLeaderboard
    let monthlyLeaderboardCollection = db.GetCollection<LeaderboardEntry> appSettings.Mongodb.Collections.MonthlyLeaderboard

    let GetNextDailyTicks() = async {
        match nextDailyTicks with
        | Some(x) -> return x // if already known just return
        | None -> 
            // if next tick is unknown, yield from db

            // get all from db
            let all = dailyCollection.Find((fun _ -> true))

            // get first ordered by id
            let ordered = all.SortByDescending((fun x -> x._id :> obj))
            let! cursor = ordered.ToCursorAsync() |> Async.AwaitTask
            let! lastPrayer = cursor.FirstAsync() |> Async.AwaitTask
            let ticks = lastPrayer._id.Value + TimeSpan.TicksPerDay

            // cache this
            nextDailyTicks <- Some ticks

            return ticks
    }

    let GetTicksTillNextDaily() = async {
        let! nextPrayerTicks = GetNextDailyTicks()

        let nextTicks = nextPrayerTicks - DateTime.Now.Ticks
        return if nextTicks > 0L then Some nextTicks else None
    }


    let RecordDaily(ID: string) = async {
        let ticks = DateTime.Now.Ticks
        let unixTicks = DateTimeOffset(DateTime.Now.ToUniversalTime()).ToUnixTimeMilliseconds();

        // store new daily 
        nextDailyTicks <- Some <| ticks + TimeSpan.TicksPerDay

        // only needed to insert to daily (the other dbs are views so are autocalculated)
        do! dailyCollection.InsertOneAsync { _id = BsonTimestamp(ticks); UnixTicks = unixTicks; DiscordID = ID } |> Async.AwaitTask

        do! Logging.recordDaily ID
    }

    // get all from this collection
    let GetLeaderboardData collection = async {
        let! cursor = IMongoCollectionExtensions.FindAsync(collection, (fun _ -> true)) |> Async.AwaitTask
        let! items = cursor.ToListAsync() |> Async.AwaitTask;
        return List.ofSeq items
    }

    let GetAlltimeLeaderboardData = GetLeaderboardData alltimeLeaderboardCollection
    let GetMonthlyLeaderboardData = GetLeaderboardData monthlyLeaderboardCollection
