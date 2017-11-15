namespace SentimentFS.AnalysisServer.WebApi

open Suave.State.CookieStateStore
open SentimentFS.AnalysisServer.Core.Tweets
module Tweets =
    open SentimentFS.AnalysisServer.Core.Config
    open Akka.Actor
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.AnalysisServer.WebApi.Storage
    open SentimentFS.AnalysisServer.Core.Tweets.TweetsMaster
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful
    open SentimentFS.AnalysisServer.Core.Tweets.Messages

    let tweetsController (system: ActorSystem) =

        let getTweetsBySearchKeys(query: string):WebPart =
            fun (x : HttpContext) ->
                async {
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result = api.Ask<Tweets option>({ key = query }) |> Async.AwaitTask
                    return! (SuaveJson.toJson result) x
                }

        let getSearchKeys(): WebPart =
            fun (x: HttpContext) ->
                async {
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result = api.Ask<string seq>(GetKeys) |> Async.AwaitTask
                    return! (SuaveJson.toJson result) x
                }


        pathStarts "/api/tweets" >=> choose [
            GET >=> choose [ pathScan "/api/tweets/key/%s" getTweetsBySearchKeys ]
            GET >=> choose [ path "/api/tweets/keys" >=>  warbler(fun _ -> getSearchKeys()) ]
        ]


