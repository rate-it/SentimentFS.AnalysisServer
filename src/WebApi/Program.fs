namespace SentimentFS.AnalysisServer.WebApi

open System
open Suave
open Akka.Actor
open Akka.Configuration
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.WebApi.Analysis
open SentimentFS.AnalysisServer.WebApi.Storage
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Tweets.TwitterApiClient

module Program =
    open SentimentFS.NaiveBayes.Dto
    open System.IO
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open SentimentFS.AnalysisServer.WebApi.Config
    open Cassandra
    open SentimentFS.AnalysisServer.Core.Tweets.TweetsStorage
    open SentimentFS.AnalysisServer.Core.Tweets.Messages

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
        let cluster = Cassandra.cluster(appconfig)
        let session = cluster |> Cassandra.session appconfig
        let dbActor = actorSystem.ActorOf(Props.Create<TweetsStorageActor>(session))
        let tweets: Tweets = { value = [ { Tweet.Zero() with Key = "test" } ] }
        dbActor.Tell(Store(tweets))
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
        Threading.Thread.Sleep(10000)
        printfn "%A" appconfig
        0
