namespace SentimentFS.AnalysisServer.WebApi

module WebServer =
    open SentimentFS.AnalysisServer.Common.Config
    open SentimentApi
    open Akka.Actor
    open Akka.Configuration
    open SentimentFS.AnalysisServer.Common.Routing
    open System.IO
    open Microsoft.Extensions.Configuration
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment
    open Microsoft.AspNetCore.Http
    open Akka.Routing
    open Giraffe

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        printfn "Cluster Node Address %A" ((actorSystem :?> ExtendedActorSystem).Provider.DefaultAddress)
        actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), Actors.router.Name) |> ignore
        choose [
            sentimentController actorSystem
        ]
