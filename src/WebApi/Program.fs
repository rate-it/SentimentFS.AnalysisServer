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
    open SentimentFS.AnalysisServer.Core.Config
    open Cassandra
    open SentimentFS.AnalysisServer.Core.Tweets.TweetsStorage
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open SentimentFS.AnalysisServer.Core.Tweets.TweetsMaster
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Core.Api

    let GetEnvVar var =
        match System.Environment.GetEnvironmentVariable(var) with
        | null -> None
        | value -> Some value

    [<EntryPoint>]
    let main argv =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let configurationRoot = ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().AddCommandLine(argv).Build();
        let appconfig = AppConfig.Zero()
        configurationRoot.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        let apiActor = actorSystem.ActorOf(Props.Create<ApiActor>(appconfig), Actors.apiActor.Name)
        let session = Cassandra.cluster appconfig |> Cassandra.session appconfig
        let tweetsActor = actorSystem.ActorOf(Props.Create<TweetsMasterActor>(session, appconfig.TwitterApiCredentials), Actors.tweetsMaster.Name)
        let analysisActor = actorSystem.ActorOf(Props.Create<AnalysisActor>(), Actors.analysisActor.Name)

        try
            WebServer.start appconfig actorSystem
            0 // return an integer exit code
        with
        | ex ->
            let color = System.Console.ForegroundColor
            System.Console.ForegroundColor <- System.ConsoleColor.Red
            System.Console.WriteLine(ex.Message)
            System.Console.ForegroundColor <- color
            1
