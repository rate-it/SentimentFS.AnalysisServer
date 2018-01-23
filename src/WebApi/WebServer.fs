namespace SentimentFS.AnalysisServer.WebApi
open Akka.Cluster.Tools.Singleton
open System.Threading
open Giraffe.Tasks

module WebServer =
    open SentimentFS.AnalysisServer.Common.Config
    open SentimentApi
    open Akka.Actor
    open Giraffe.HttpHandlers
    open Akka.Configuration
    open Api.Actor
    open SentimentFS.AnalysisServer.Common.Routing
    open System.IO
    open Microsoft.Extensions.Configuration
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment
    open Giraffe.Tasks
    open Giraffe.HttpHandlers
    open Giraffe.HttpContextExtensions
    open Microsoft.AspNetCore.Http
    open Akka.Routing

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        printfn "Cluster Node Address %A" ((actorSystem :?> ExtendedActorSystem).Provider.DefaultAddress)
        let router = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), Actors.router.Name)
        choose [
            sentimentController actorSystem
        ]
