namespace SentimentFS.AnalysisServer.WebApi

// module Analysis =
//     open Akka.Actor
//     open SentimentFS.AnalysisServer.Core.Analysis
//     open SentimentFS.AnalysisServer.Core.Sentiment
//     open SentimentFS.AnalysisServer.Core.Actor
//     open Cassandra
//     open Tweetinvi
//     open System.Net.Http
//     open Newtonsoft.Json
//     open SentimentFS.AnalysisServer.Core.Config

//     let analysisController(system: ActorSystem) =
//         let getAnalysisResultByKey(key):WebPart =
//             fun (x : HttpContext) ->
//                 async {
//                     let api = system.ActorSelection(Actors.apiActor.Path)
//                     let! result = api.Ask<AnalysisScore option>({ searchKey = key }) |> Async.AwaitTask
//                     return! (SuaveJson.toJson result) x
//                 }

//         pathStarts "/api/analysis" >=> choose [
//             GET >=> choose [ pathScan "/api/analysis/result/%s" getAnalysisResultByKey ]
//         ]
