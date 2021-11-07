namespace Bot

open System.IO
open System

module Logging = 
    let logSrc = "log"

    let ensureLogExists =
        if not <| File.Exists logSrc then
            (File.Create logSrc).Close()

    let printTime = async {
        let logLines = [
            $"{DateTime.Now.ToLongDateString()}"; 
            $"{DateTime.Now.ToLongTimeString()}"; 
        ]
        do! File.AppendAllLinesAsync (logSrc, logLines) |> Async.AwaitTask
    }

    let recordDaily id = async {
        ensureLogExists
        do! printTime
        do! File.AppendAllLinesAsync (logSrc, [$"msg by %s{id}"]) |> Async.AwaitTask
    }

    let recordExn (exn: exn) = async {
        ensureLogExists
        do! printTime
        do! File.AppendAllLinesAsync (logSrc, [exn.ToString()]) |> Async.AwaitTask
    }