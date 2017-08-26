namespace SentimentFS.AnalysisServer.WebApi

open System
open Suave
open SentimentFS.AnalysisServer

module Program =

    let GetEnvVar var =
        match System.Environment.GetEnvironmentVariable(var) with
        | null -> None
        | value -> Some value

    let getPortsOrDefault defaultVal =
        match System.Environment.GetEnvironmentVariable("APP_PORT") with
        | null -> defaultVal
        | value -> value |> uint16

    [<EntryPoint>]
    let main argv =
        try
            WebServer.start (getPortsOrDefault 8080us)
            0 // return an integer exit code
        with
        | ex ->
            let color = System.Console.ForegroundColor
            System.Console.ForegroundColor <- System.ConsoleColor.Red
            System.Console.WriteLine(ex.Message)
            System.Console.ForegroundColor <- color
            1
