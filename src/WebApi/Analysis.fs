namespace SentimentFS.AnalysisServer.WebApi

module Analysis =
    open Akka.Actor
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Core.Sentiment
    open SentimentFS.AnalysisServer.Core.Actor
    open Cassandra
    open Tweetinvi
    open System.Net.Http
    open Newtonsoft.Json
    open SentimentFS.AnalysisServer.WebApi.Config

    let analysisController(config: AppConfig)(system: ActorSystem) =
        let analysisActor =
            system.ActorOf(Props.Create<AnalysisActor>(), Actors.analysisActor.Name)

        let getAnalysisResultByKey(key):WebPart =
            fun (x : HttpContext) ->
                async {
                    let! result = analysisActor.Ask<string>({ key = key }) |> Async.AwaitTask
                    return! OK result x
                }

        pathStarts "/api/analysis" >=> choose [
            GET >=> choose [ pathScan "/api/analysis/result/%s" getAnalysisResultByKey ]
        ]
