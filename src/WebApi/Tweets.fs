namespace SentimentFS.AnalysisServer.WebApi

module Tweets =
    open Akka.Actor
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open Suave
    open Filters
    open Operators

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


