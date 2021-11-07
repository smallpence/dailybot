// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Bot

[<EntryPoint>]
let main argv =
    Main.run |> Async.RunSynchronously
    0