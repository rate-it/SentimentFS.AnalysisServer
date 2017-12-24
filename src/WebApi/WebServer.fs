namespace SentimentFS.AnalysisServer.WebApi
open Akka.Cluster.Tools.Singleton
open System.Threading

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
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment

    open Akka.Routing

    let app (config: IConfigurationRoot) =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let appconfig = AppConfig.Zero()
        config.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig.WithFallback(ClusterSingletonManager.DefaultConfig()))
        printfn "Cluster Node Address %A" ((actorSystem :?> ExtendedActorSystem).Provider.DefaultAddress)
        let router = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), Actors.router.Name)
        Thread.Sleep(10000)
        router.Tell(SentimentCommand(Train({ value = "Testr"; category = Emotion.Positive; weight = None })))
        choose [
            sentimentController actorSystem
        ]
