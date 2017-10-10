namespace SentimentFS.AnalysisServer.WebApi

module Tweets =
    open SentimentFS.AnalysisServer.WebApi.Config
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

    let tweetsController (config: AppConfig) (system: ActorSystem) =
        let session = Cassandra.cluster config |> Cassandra.session config
        let tweetsActor = system.ActorOf(Props.Create<TweetsMasterActor>(session, config.TwitterApiCredentials), Actors.tweetsMaster.Name)

        let getTweetsBySearchKeys(query: string):WebPart =
            fun (x : HttpContext) ->
                async {
                    let! result = tweetsActor.Ask<Tweets option>({ key = query }) |> Async.AwaitTask
                    return! (SuaveJson.toJson result) x
                }


        pathStarts "/api/tweets" >=> choose [
            GET >=> choose [ pathScan "/api/tweets/%s" getTweetsBySearchKeys ]
        ]


