namespace SentimentFS.AnalysisServer.WebApi

open Microsoft.Extensions.Configuration
module WebServer =

    open SentimentApi
    open SentimentFS.AnalysisServer.Core.Config
    open Akka.Actor
    open Giraffe.HttpHandlers
    open Akka.Configuration
    open SentimentFS.AnalysisServer.WebApi.Storage
    open System.IO
    open SentimentFS.AnalysisServer.Core.Api
    open SentimentFS.AnalysisServer.Core.Actor

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        use actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        use session = Cassandra.cluster appconfig |> Cassandra.session appconfig
        let apiActor = actorSystem.ActorOf(Props.Create<ApiMasterActor>(appconfig, session), Actors.apiActor.Name)
        choose [
            sentimentController actorSystem
        ]
