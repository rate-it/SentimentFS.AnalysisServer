namespace SentimentFS.AnalysisServer

open Common.Messages.Twitter
open Akka.Streams.Dsl
open Akkling.Streams
open Akka
open Tweetinvi.Parameters
open System
open Tweetinvi.Models
open Tweetinvi
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open Akkling
open Akka.Streams.Dsl
open Akka.Streams
open Akka.Actor

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
        |> Flow.asyncMapUnordered(maxConcurrentDownloads)(fun q ->
                                         async {
                                            let options = SearchTweetsParameters(q.key)
                                            options.SearchType <- Nullable<SearchResultType>(SearchResultType.Recent)
                                            options.Lang <- Nullable<LanguageFilter>(LanguageFilter.English)
                                            options.Filters <- TweetSearchFilters.None
                                            options.MaximumNumberOfResults <- q.quantity
                                            options.Since <- q.since
                                            return! SearchAsync.SearchTweets(options) |> Async.AwaitTask
                                         })
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

    let sentimentFlow (maxConcurentSentimentRequest)(sentimentActor: IActorRef<SentimentMessage>) =
        Flow.id
        |> Flow.asyncMapUnordered(maxConcurentSentimentRequest)(fun tweet ->
                                                                    async {
                                                                        let! s = sentimentActor <? SentimentCommand(Classify({ text = tweet.Text }))
                                                                        let r = s.score |> Array.maxBy(fun res -> res.probability)
                                                                        return { tweet with Sentiment = Some r.emotion }
                                                                    }
                                                                )

    let trainSink(sentimentActor: IActorRef<SentimentMessage>) =
        Sink.forEachParallel(MaxConcurrentDownloads)(fun tweet ->
                            sentimentActor <! SentimentCommand(Train({ value = tweet.Text; category = defaultArg tweet.Sentiment Emotion.Neutral; weight = None  }))
                        )

    let searchActorSource(credentials: TwitterCredentials)(sentimentActor: IActorRef<SentimentMessage>)  =
        let apiSearchSource = Source.actorRef(OverflowStrategy.DropNew)(1000)
        let graph = Graph.create(fun builder ->

                            let downloadFlow = builder.Add(Flow.id |> Flow.via(downloadTweetsFlow(MaxConcurrentDownloads)(credentials)) |> Flow.via(sentimentFlow(MaxConcurrentDownloads)(sentimentActor)))
                            let broadcast = builder.Add(Broadcast(2))
                            builder.From(downloadFlow).To(broadcast.In) |> ignore
                            builder.From(broadcast.Out(0)).To(trainSink(sentimentActor)) |> ignore
                            FlowShape(downloadFlow.Inlet, broadcast.Out(1))
                       )
        apiSearchSource |> Source.via graph

    let storeToDbSink(dagreeOfParalellism)(store: Tweet -> Async<unit>) =
        Sink.forEachParallel(dagreeOfParalellism)(store >> Async.RunSynchronously)


    let twitterApiActor (mailbox: Actor<TwitterApiMessage>) =
        let rec loop () = actor {
            let! msg = mailbox.Receive()
            match msg with
            | SearchTweets q ->
                return loop()
            return loop()
        }
        loop ()

module Actor =

    let tweets (mailbox: Actor<TweetsMessage>)(credentials: TwitterCredentials) =
        let sentimentActor = mailbox.System.ActorSelection("")
        let twitterApiSearchActorSource = TwitterApi.searchActorSource(credentials)(sentimentActor.Anchor :?> IActorRef<SentimentMessage>)
        let rec loop (state) =
            actor {
                let! msg = mailbox.Receive()
                return loop({tweets = []})
            }
        loop({tweets = []})



