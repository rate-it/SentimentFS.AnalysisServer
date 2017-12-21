namespace SentimentFS.AnalysisServer.WebApi

open Microsoft.Extensions.Configuration
module WebServer =
    open SentimentFS.AnalysisServer.Common.Config
    open SentimentApi
    open Akka.Actor
    open Giraffe.HttpHandlers
    open Akka.Configuration
    open Api.Actor
    open SentimentFS.AnalysisServer.WebApi.Storage
    open System.IO

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        let cluster = Cassandra.cluster appconfig
        let apiActor = actorSystem.ActorOf(Props.Create<ApiActor>(appconfig, cluster), "")
        choose [
            sentimentController actorSystem
        ]
