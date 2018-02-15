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
            options.MaximumNumberOfResults <- 10
            options.Since <- DateTime.Now
            return! SearchAsync.SearchTweets(options) |> Async.AwaitTask
        }



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

    let twitterApiActor(config: Config)(mailbox: Actor<TwitterApiActorMessage>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | ApiSearch search ->
                    let sentimentActor:TypedActorSelection<SentimentMessage> = select mailbox.System (Actors.sentimentRouter.Path)
                    let tweets =
                        Source.ofAsync (TwitterApi.downloadTweetsFromApi search)
                            |> Source.collect(id)
                            |> Source.filter(fun tweet -> not tweet.IsRetweet)
                            |> Source.map(fun tweet -> { IdStr = tweet.IdStr;
                                                         Text = tweet.Text;
                                                         Language = tweet.Language.ToString();
                                                         CreationDate = tweet.CreatedAt;
                                                         Coordinates = match tweet.Coordinates with null -> None | coord -> Some { Longitude = coord.Longitude; Latitude = coord.Latitude };
                                                         HashTags = (tweet.Hashtags |> Seq.map(fun x -> x.Text))
                                                         User = tweet.CreatedBy.UserDTO.Name
                                                         Sentiment = None })
                            |> Source.asyncMapUnordered(500)(fun tweet ->
                                                                    async {
                                                                        let! s = sentimentActor <? SentimentCommand(Classify({ text = tweet.Text }))
                                                                        let r = s.score |> Array.maxBy(fun res -> res.probability)
                                                                        return { tweet with Sentiment = Some r.emotion }
                                                                    }
                                                                )
                    return loop()
            }
        loop()

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

    let postgresTweetsStorageActor(mailbox: Actor<TweetsStorageActorMessage>)(connectionString: string) =
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
    let tweetMasterActor(mailbox: Actor<TweetsActorMessage>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | SearchByKey key ->
                    return! loop()
            }
        loop()
