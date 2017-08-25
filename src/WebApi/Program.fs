namespace SentimentFS.WebApi

open System
open Suave
module Program =

    [<EntryPoint>]
    let main argv =
        startWebServer defaultConfig (Successful.OK "Hello World!")
        0 // return an integer exit code
