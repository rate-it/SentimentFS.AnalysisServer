namespace SentimentFS.AnalysisServer.WebApi

module Analysis =
    open Akka.Actor
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Core.Actor
    open JSON
    open Giraffe
    open Giraffe.Tasks
    open Giraffe.HttpHandlers
    open Giraffe.HttpContextExtensions
    open Microsoft.AspNetCore.Http

    let analysisController(system: ActorSystem) =
        let getAnalysisResultByKey(key) =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result = api.Ask<AnalysisScore option>({ searchKey = key })
                    return! customJson settings result next ctx
                }

        routeStartsWith  "/api/analysis" >=> choose [
            GET >=> routef  "/api/analysis/result/%s" getAnalysisResultByKey
        ]
