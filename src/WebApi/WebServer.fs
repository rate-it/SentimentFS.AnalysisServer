namespace SentimentFS.AnalysisServer.WebApi

module WebServer =
    open SentimentFS.AnalysisServer.Common.Config
    open SentimentApi
    open Akka.Actor
    open Giraffe.HttpHandlers
    open Akka.Configuration
    open Api.Actor
    open SentimentFS.AnalysisServer.WebApi.Storage
    open SentimentFS.AnalysisServer.Common.Routing
    open System.IO
    open Microsoft.Extensions.Configuration
    open Akka.Routing

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        let router = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), Actors.router.Name)
        choose [
            sentimentController actorSystem
        ]
