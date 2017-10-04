namespace SentimentFS.AnalysisServer.WebApi

open System
open Suave
open Akka.Actor
open Akka.Configuration
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.WebApi.Analysis
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.AnalysisServer.Core.Actor

module Program =
    open SentimentFS.NaiveBayes.Dto
    open System.IO

    let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka.json"))
    let actorSystem =
            ActorSystem.Create("sentimentfs", akkaConfig)

    let sentimentActor =
            actorSystem.ActorOf(Props.Create<SentimentActor>(Some defaultClassificatorConfig), Actors.sentimentActor.Name)

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
        // try
        //     WebServer.start (getPortsOrDefault 8080us)
        //     0 // return an integer exit code
        // with
        // | ex ->
        //     let color = System.Console.ForegroundColor
        //     System.Console.ForegroundColor <- System.ConsoleColor.Red
        //     System.Console.WriteLine(ex.Message)
        //     System.Console.ForegroundColor <- color
        //     1
        (initSentimentActor("https://raw.githubusercontent.com/wooorm/afinn-96/master/index.json") sentimentActor)
        printfn "%A" (sentimentActor.Ask<ClassificationScore<Emotion>>({ text = "My brother hate java" }) |> Async.AwaitTask |> Async.RunSynchronously)
        0
