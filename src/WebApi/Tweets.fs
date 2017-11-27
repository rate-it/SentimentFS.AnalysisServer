namespace SentimentFS.AnalysisServer.WebApi

module Tweets =
    open Akka.Actor
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open JSON
    open Giraffe
    open Giraffe.Tasks
    open Giraffe.HttpHandlers
    open Giraffe.HttpContextExtensions
    open Microsoft.AspNetCore.Http

    let tweetsController (system: ActorSystem) =
        let getTweetsBySearchKeys(query: string) =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result = api.Ask<Tweets option>({ key = query })
                    return! customJson settings result next ctx
                }

        let getSearchKeys =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result = api.Ask<string seq>(GetKeys)
                    return! customJson settings result next ctx
                }


        routeStartsWith  "/api/tweets" >=> choose [
            GET >=> routef "/api/tweets/key/%s" getTweetsBySearchKeys
            GET >=> route "/api/tweets/keys" >=> getSearchKeys
        ]


