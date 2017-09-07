namespace SentimentFS.AnalysisServer.Core

module TweetsStorage =
    open SentimentFS.AnalysisServer.Domain.Tweets
    open MongoDB.Driver
    open MongoDB.Bson
    open MongoDB.Driver
    open MongoDB.Driver.Linq

    let private TweetsCollection (db:IMongoDatabase) = db.GetCollection<Tweet>("tweets")

    let private store (tweets: Tweets) (col: IMongoCollection<Tweet>) =
        async {
            return! col.InsertManyAsync(tweets.value) |> Async.AwaitTask
        }

    let private getByKey (key:string) (col: IMongoCollection<Tweet>) =
        async {
            let! result = col.Find(fun tweet -> tweet.Key = key).ToListAsync() |> Async.AwaitTask
            return match result |> Seq.toList with
                   | [] -> None
                   | l -> Some { value = l }
        }

    let private getSearchKeys (col: IMongoCollection<Tweet>) =
        async {
            let! result = col.Distinct<string>(FieldDefinition<_,_>.op_Implicit("Key"), FilterDefinition.op_Implicit("{}")).ToListAsync() |> Async.AwaitTask
            return match result |> Seq.toList with
                   | [] -> None
                   | l -> Some l
        }

    let spawn(db: IMongoDatabase) =
        MailboxProcessor.Start(fun agent ->
            let rec loop() = async {
                let! msg = agent.Receive()
                match msg with
                | Store tweets ->
                    do! db |> TweetsCollection |> store(tweets)
                    return! loop()
                | GetByKey(key, reply) ->
                    let! res = db |> TweetsCollection |> getByKey key
                    reply.Reply(res)
                    return! loop()
                | GetSearchKeys(reply) ->
                    let! res = db |> TweetsCollection |> getSearchKeys
                    res |> reply.Reply |> ignore
                    return! loop()

            }
            loop()
        )

module TwitterApiClient =
    open SentimentFS.AnalysisServer.Domain.Tweets
    open SentimentFS.AnalysisServer.Domain.Sentiment
    open System
    open Tweetinvi
    open Tweetinvi.Models
    open Tweetinvi.Parameters

    let spawn(credentials: ITwitterCredentials) =
        MailboxProcessor.Start(fun agent ->
            let rec loop () =
                async {
                    let! msg = agent.Receive()
                    match msg with
                    | GetTweets(key, reply) ->
                        let options = SearchTweetsParameters(key)
                        options.SearchType <- Nullable<SearchResultType>(SearchResultType.Recent)
                        options.Lang <- Nullable<LanguageFilter>(LanguageFilter.English)
                        options.Filters <- TweetSearchFilters.None
                        options.MaximumNumberOfResults <- 1000
                        let! queryResult = SearchAsync.SearchTweets(options) |> Async.AwaitTask
                        let result = queryResult
                                        |> Seq.map(fun tweet -> { IdStr = tweet.TweetDTO.IdStr; Text = tweet.TweetDTO.Text; Lang = tweet.TweetDTO.Language.ToString();  Key = key; Date = x.TweetDTO.CreatedAt; Longitude = 0.0; Latitude = 0.0; Sentiment = Sentiment.Neutral })
                                        |> Seq.toList
                        match result with
                        | [] -> reply.Reply(None)
                        | list -> reply.Reply(Some { value = list })
                    return! loop()
                }
            loop()
        )
