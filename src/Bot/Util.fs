namespace Bot
open System

module StringUtil = 
    let SomeIf b opt =
        Option.bind (fun x -> if b then Some x else None) opt

    let FormatTimeP p time timestr =
        if p then Some $"""{time} %s{timestr}{if time <> 1 then "s" else ""}""" else None

    let FormatTime time timestr =
        FormatTimeP (time > 0) time timestr

    let TicksToString ticks seconds minutes hours = 
        if not (seconds || minutes || hours) then raise <| ArgumentException "one bool needs to be true"

        let time = DateTime(ticks)
        
        let timeStrings = List.choose id [
            SomeIf hours <| FormatTime time.Hour "hour";
            SomeIf minutes <| FormatTime time.Minute "minute";
            SomeIf seconds <| FormatTimeP true time.Second "second";
        ]

        // remove all of the 'none' elements
        // if no items, put the first of the enabled in there
        // excluding seconds - 
        let timeStrings = if timeStrings.Length > 0 then timeStrings else [List.pick id [ SomeIf minutes <| Some "0 minutes"; SomeIf hours <| Some "0 hours"]]
            
        String.concat ", " timeStrings