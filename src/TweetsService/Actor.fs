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
    open Dto
    type Config = { credentials: TwitterCredentials; }

    let inMemoryTweetsStorageActor(mailbox: Actor<TweetsStorageActorMessage>) =
        let rec loop (tweets: TweetDto list) =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | Insert tweet ->
                    return! loop(Dto.TweetDto.FromTweet(tweet) :: tweets)
                | Search q ->
                    let result = tweets |> List.filter(fun x -> x.Text.Contains(q.key)) |> List.map(TweetDto.ToTweet) |> List.toSeq
                    if result |> Seq.isEmpty then
                        mailbox.Sender() <! None
                    else
                        mailbox.Sender() <! Some result
                    return! loop(tweets)
            }
        loop([])

