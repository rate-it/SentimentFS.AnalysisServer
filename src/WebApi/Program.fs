namespace SentimentFS.AnalysisServer.WebApi

open Akka.Actor
open Akka.Configuration
open SentimentFS.AnalysisServer.WebApi.Storage
open SentimentFS.AnalysisServer.Core.Actor

module Program =
    open System.IO
    open SentimentFS.AnalysisServer.Core.Config
    open SentimentFS.AnalysisServer.Core.Api
    open Microsoft.Extensions.Configuration

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
        let session = Cassandra.cluster appconfig |> Cassandra.session appconfig
        let apiActor = actorSystem.ActorOf(Props.Create<ApiActor>(appconfig, session), Actors.apiActor.Name)

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
