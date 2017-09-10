namespace SentimentFS.AnalysisServer.WebApi

module Analysis =
    open Akka.Actor
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Domain.Analysis

    let analysisController() =
        let actorSystem =
            ActorSystem.Create("sentimentfs")

        let analysisActor =
            actorSystem.ActorOf(Props.Create<AnalysisActor>())

        let getAnalysisResultByKey(key):WebPart =
            fun (x : HttpContext) ->
                async {
                    do analysisActor.Tell({ key = key })
                    return! OK key x
                }

        pathStarts "/api/analysis" >=> choose [
            GET >=> choose [ pathScan "/api/analysis/result/%s" getAnalysisResultByKey ]
        ]
