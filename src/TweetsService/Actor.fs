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

module Actor =
    open TwitterApi
    type Config = { credentials: TwitterCredentials; }

    let twitterApiActor(config: Config)(mailbox: Actor<TwitterApiActorMessage>) =
        let apiSource = Source.actorRef(OverflowStrategy.DropNew)(5000)
        let resultSink = Sink.toActorRef (Complete)(mailbox.Self)
        let graph system = apiSource
                            |> Graph.create1 (fun builder s ->
                                            let sentimentActor:TypedActorSelection<SentimentMessage> = select system (Actors.sentimentRouter.Path)
                                            let downloadTweetsFlow = downloadTweetsFlow(MaxConcurrentDownloads)(config.credentials) |> builder.Add
                                            let sentimentFlow = sentimentFlow(MaxConcurrentDownloads)(sentimentActor) |> builder.Add
                                            builder.From(s.Outlet).To(downloadTweetsFlow.Inlet) |> ignore
                                            builder.From(downloadTweetsFlow.Outlet).Via(sentimentFlow).Via(Flow.id |> Flow.map(Receive)).To(resultSink) |> ignore
                                            ClosedShape.Instance
                                        )
        let twitter = graph(mailbox.System) |> Graph.runnable |> Graph.run (mailbox.Materializer())

        let rec loop() =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | ApiSearch search ->
                    twitter <! search
                | Receive tweet ->
                    let sentimentActor:TypedActorSelection<SentimentMessage> = select mailbox.System (Actors.sentimentRouter.Path)
                    let tweetsActor: TypedActorSelection<TweetsStorageActorMessage> = select mailbox.System (Actors.tweetsActor.Path)
                    sentimentActor <! SentimentCommand(Train({ value = tweet.Text; category = defaultArg tweet.Sentiment Emotion.Neutral; weight = None  }))
                    tweetsActor <! Insert tweet
                | Complete ->
                    return loop()
                return loop()
            }
        loop()

    let tweetsActor (db: ITweetsRepository)(mailbox: Actor<TweetsStorageActorMessage>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | Insert tweet ->
                    do! db.StoreAsync(Dto.TweetDto.FromTweet(tweet))
                    return! loop()
                | Search q ->
                    let result = db.GetAsync(q) |> Async.RunSynchronously
                    if result |> Seq.isEmpty then
                        mailbox.Sender() <! None
                    else
                        mailbox.Sender() <! Some result
                    return! loop()
                return! loop()
            }
        loop()



