namespace SentimentFS.AnalysisServer

open Common.Messages.Twitter
open Akka.Streams.Dsl
open Akkling.Streams
open Tweetinvi.Parameters
open System
open Tweetinvi.Models
open Tweetinvi
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open Akkling
open Akka.Streams

module Actors =
    open Common.Routing.ActorMetaData
    let tweetsActor = create("tweets", None)
    let sentimentRouter = create("sentiment", None)
    let twitterApiActor = create("twitter-api", None)

module TwitterApi =

    [<Literal>]
    let MaxConcurrentDownloads = 5000

    let downloadTweetsFromApi q =
        async {
            let options = SearchTweetsParameters(q.key)
            options.SearchType <- Nullable<SearchResultType>(SearchResultType.Recent)
            options.Lang <- Nullable<LanguageFilter>(LanguageFilter.English)
            options.Filters <- TweetSearchFilters.None
            options.MaximumNumberOfResults <- q.quantity
            options.Since <- q.since
            return! SearchAsync.SearchTweets(options) |> Async.AwaitTask
        }

    let downloadTweetsFlow (maxConcurrentDownloads: int)(credentials: TwitterCredentials) =
        Flow.id
        |> Flow.asyncMapUnordered(maxConcurrentDownloads)(downloadTweetsFromApi)
        |> Flow.collect(id)
        |> Flow.filter(fun tweet -> not tweet.IsRetweet)
        |> Flow.map(fun tweet ->
                        { IdStr = tweet.IdStr;
                          Text = tweet.Text;
                          Language = tweet.Language.ToString();
                          CreationDate = tweet.CreatedAt;
                          Coordinates = match tweet.Coordinates with null -> None | coord -> Some { Longitude = coord.Longitude; Latitude = coord.Latitude };
                          HashTags = (tweet.Hashtags |> Seq.map(fun x -> x.Text))
                          Sentiment = None })


    let sentimentFlow (maxConcurentSentimentRequest)(sentimentActor: ICanTell<SentimentMessage>) =
        Flow.id
        |> Flow.asyncMapUnordered(maxConcurentSentimentRequest)(fun tweet ->
                                                                    async {
                                                                        let! s = sentimentActor <? SentimentCommand(Classify({ text = tweet.Text }))
                                                                        let r = s.score |> Array.maxBy(fun res -> res.probability)
                                                                        return { tweet with Sentiment = Some r.emotion }
                                                                    }
                                                                )

    let trainSink(sentimentActor: ICanTell<SentimentMessage>) =
        Sink.forEachParallel(MaxConcurrentDownloads)(fun tweet ->
                            sentimentActor <! SentimentCommand(Train({ value = tweet.Text; category = defaultArg tweet.Sentiment Emotion.Neutral; weight = None  }))
                        )

    let insertToDbActorSink(tweetsActor: ICanTell<TweetsMessage>) =
        Sink.forEachParallel(MaxConcurrentDownloads)(fun tweet -> tweetsActor <! Insert tweet)

module Actor =
    open TwitterApi
    type Config = { credentials: TwitterCredentials; }

    let twitterApiActor(config: Config)(mailbox: Actor<SearchTweets>) =
        let apiSource = Source.actorRef(OverflowStrategy.DropNew)(5000)
        let graph system = apiSource
                            |> Graph.create1 (fun builder s ->
                                            let sentimentActor:TypedActorSelection<SentimentMessage> = select system (Actors.sentimentRouter.Path)
                                            let tweetsActor: TypedActorSelection<TweetsMessage> = select system (Actors.tweetsActor.Path)
                                            let downloadTweetsFlow = builder.Add(downloadTweetsFlow(MaxConcurrentDownloads)(config.credentials))
                                            let sentimentFlow = builder.Add(sentimentFlow(MaxConcurrentDownloads)(sentimentActor))
                                            let tweetBroadcast = builder.Add(Broadcast(2))

                                            builder.From(s.Outlet).To(downloadTweetsFlow.Inlet) |> ignore
                                            builder.From(downloadTweetsFlow.Outlet).Via(sentimentFlow).To(tweetBroadcast.In) |> ignore
                                            builder.From(tweetBroadcast.Out(0)).To(trainSink(sentimentActor)) |> ignore
                                            builder.From(tweetBroadcast.Out(1)).To(insertToDbActorSink(tweetsActor)) |> ignore
                                            ClosedShape.Instance
                                        )
        let twitter = graph(mailbox.System) |> Graph.runnable |> Graph.run (mailbox.Materializer())

        let rec loop() =
            actor {
                let! msg = mailbox.Receive()
                twitter <! msg
                return loop()
            }
        loop()

    let tweetsActor (db: ITweetsRepository)(mailbox: Actor<TweetsMessage>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                printfn "%A" msg
                match msg with
                | Insert tweet ->
                    do! db.StoreAsync(Dto.TweetDto.FromTweet(tweet))
                    return! loop()
                | Search q ->
                    printfn "db %A" db
                    let! result = db.GetAsync(q)
                    printfn "Result %A" msg
                    if result |> Seq.isEmpty then
                        mailbox.Sender() <! None
                    else
                        mailbox.Sender() <! Some result
                    return! loop()
                return! loop()
            }
        loop()



