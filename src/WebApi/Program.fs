namespace SentimentFS.AnalysisServer.WebApi

open System
open Suave
open Akka.Actor
open Akka.Configuration
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.WebApi.Analysis
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Tweets.TwitterApiClient

module Program =
    open SentimentFS.NaiveBayes.Dto
    open System.IO
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open SentimentFS.AnalysisServer.WebApi.Config
    open Cassandra

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
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let configurationRoot = ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().AddCommandLine(argv).Build();
        let appconfig = AppConfig.Zero()
        configurationRoot.Bind(appconfig) |> ignore

        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        let cluster =
            Cluster
                .Builder()
                .AddContactPoint("127.0.0.1")
                .WithDefaultKeyspace("sentiment_fs")
                .Build()

        let session = cluster.ConnectAndCreateDefaultKeyspaceIfNotExists()
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
        printfn "%A" appconfig
        0
