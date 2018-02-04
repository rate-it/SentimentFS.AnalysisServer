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

module Actor =
    open TwitterApi
    type Config = { credentials: TwitterCredentials; sentimentActorPath: string }

    let tweetsActor (db: ITweetsRepository)(config: Config)(mailbox: Actor<TweetsMessage>) =
        let apiSearchSource = Source.actorRef(OverflowStrategy.DropNew)(1000)
        let selfDbSink = Sink.forEachParallel(50) (fun tweet -> mailbox.Self <! Insert tweet )
        let apiGraph = apiSearchSource |> Graph.create1(fun builder s ->
                                                            let sentimentActor:TypedActorSelection<SentimentMessage> = select mailbox.System config.sentimentActorPath
                                                            let downloadFlow = builder.Add(Flow.id
                                                                                           |> Flow.via(downloadTweetsFlow(MaxConcurrentDownloads)(config.credentials))
                                                                                           |> Flow.via(sentimentFlow(MaxConcurrentDownloads)(sentimentActor)))
                                                            let tweetsBroadcast = builder.Add(Broadcast(2))
                                                            builder.From(s.Outlet).Via(downloadFlow).To(tweetsBroadcast.In) |> ignore
                                                            builder.From(tweetsBroadcast.Out(0)).To(trainSink(sentimentActor)) |> ignore
                                                            builder.From(tweetsBroadcast.Out(1)).To(selfDbSink) |> ignore
                                                            ClosedShape.Instance
                                                        )
        let apiActor = apiGraph |> Graph.runnable |> Graph.run (mailbox.Materializer())
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | Insert tweet ->
                    do! db.StoreAsync(Dto.TweetDto.FromTweet(tweet))
                    return loop()
                | Search q ->
                    let! result = db.GetAsync(q)
                    if result |> Seq.isEmpty then
                        apiActor <! q
                        mailbox.Sender() <! None
                    else
                        mailbox.Sender() <! Some result
                    return loop()
                return loop()
            }
        loop()



