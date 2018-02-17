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
open Akkling.Streams
open Npgsql

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

module Actor =
    open Dto
    type Config = { credentials: TwitterCredentials; }

    let twitterApiActor(config: Config)(mailbox: Actor<TwitterApiActorMessage>) =
        let credentials = Auth.SetUserCredentials(config.credentials.ConsumerKey, config.credentials.ConsumerSecret, config.credentials.AccessToken, config.credentials.AccessTokenSecret)
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | ApiSearch search ->
                    let sentimentActor:TypedActorSelection<SentimentMessage> = select mailbox.System (Actors.sentimentRouter.Path)
                    let! tweets =
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
                            |> Source.runWith (mailbox.System.Materializer()) (Sink.fold ([]) ( fun acc x -> x :: acc))

                    mailbox.Sender() <! tweets
                    return loop()
            }
        loop()

    let inMemoryTweetsStorageActor(mailbox: Actor<TweetsStorageActorMessage>) =
        let rec loop (tweets: TweetDto list) =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | InsertOne tweet ->
                    return! loop(Dto.TweetDto.FromTweet(tweet) :: tweets)
                | InsertMany tweetList ->
                    return! loop((tweetList |> List.map(fun tweet -> Dto.TweetDto.FromTweet(tweet))) @ tweets)
                | Search q ->
                    let result = tweets |> List.filter(fun x -> x.Text.Contains(q.key)) |> List.map(TweetDto.ToTweet) |> List.toSeq
                    if result |> Seq.isEmpty then
                        mailbox.Sender() <! None
                    else
                        mailbox.Sender() <! Some result
                    return! loop(tweets)
            }
        loop([])

    let postgresTweetsStorageActor(connectionString: string)(mailbox: Actor<TweetsStorageActorMessage>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | InsertOne tweet ->
                    use connection = new NpgsqlConnection(connectionString)
                    do! Postgres.insertTweet(connection)(Dto.TweetDto.FromTweet(tweet))
                    return! loop()
                | InsertMany tweetList ->
                    use connection = new NpgsqlConnection(connectionString)
                    do! Postgres.insertTweets(connection)(tweetList |> List.map(fun tweet -> Dto.TweetDto.FromTweet(tweet)) |> List.toArray)
                    return! loop()
                | Search q ->
                    return! loop()
            }
        loop()

    let tweetsMasterActor(readActorProps: Props<TweetsStorageActorMessage>)(writeActorsProps: Props<TweetsStorageActorMessage> list)(mailbox: Actor<TweetsActorMessage>) =
        let tweetReadActor = spawnAnonymous mailbox.System readActorProps
        let writeActors = writeActorsProps |> List.map(fun prop -> spawnAnonymous mailbox.System prop)
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | SearchByKey key ->
                    let! tweetsOpt = tweetReadActor <? TweetsStorageActorMessage.Search({ key = key; since = None; quantity = None })
                    match tweetsOpt with
                    | Some tweets ->
                        mailbox.Sender() <! tweets
                    | None ->
                        mailbox.Sender() <! Seq.empty
                    return! loop()
                | Save tweets ->
                    for writeActor in writeActors do
                        writeActor <! InsertMany tweets
                    return loop()

            }
        loop()
