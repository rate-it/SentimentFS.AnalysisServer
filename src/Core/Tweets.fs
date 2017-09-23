namespace SentimentFS.AnalysisServer.Core

module TweetsStorage =
    open System
    open SentimentFS.AnalysisServer.Domain.Tweets
    open SentimentFS.AnalysisServer.Domain.Sentiment
    open Cassandra
    open Cassandra.Data
    open Cassandra.Mapping
    open Akka.Actor

    let private createTweetsCollectionIfNotExists (session: ISession) =
        session.Execute("""
                          CREATE TABLE IF NOT EXISTS tweets (
                            id uuid,
                            id_str varchar,
                            text text,
                            key varchar,
                            date timestamp,
                            lang varchar,
                            longitude double,
                            latitude double,
                            sentiment int,
                            PRIMARY KEY(key, id)
                          );
                        """)

    let private store (tweets: Tweets) (session: ISession) =
        async {
            let batch = BatchStatement()
            let query = session.Prepare("""
                            INSERT INTO tweets (id, id_str, text, key, date, lang, longitude, latitude, sentiment) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                        """)
            for tweet in tweets.value do
                query.Bind(tweet.Id, tweet.IdStr, tweet.Text, tweet.Key, tweet.Date, tweet.Lang, tweet.Longitude, tweet.Latitude, tweet.Sentiment) |> batch.Add |> ignore

            session.ExecuteAsync(batch) |> Async.AwaitTask |> ignore
        }

    let private getByKey (key:string) (session: ISession) =
        async {
            let! query = session.PrepareAsync("SELECT id, id_str, text, key, date, lang, longitude, latitude, sentiment FROM tweets WHERE key=?") |> Async.AwaitTask
            let! result = session.ExecuteAsync(query.Bind(key)) |> Async.AwaitTask
            return match (result.GetRows()) |> Seq.toList with
                   | [] -> None
                   | l -> Some { value = (l |> List.map(fun x -> x |> Tweet.FromCassandraRow )) }
        }

    let private getSearchKeys (session: ISession) =
        async {
            let q = SimpleStatement("SELECT DISTINCT key FROM tweets;")
            let! result = session.ExecuteAsync(q.SetPageSize(100)) |> Async.AwaitTask
            return match (result.GetRows()) |> Seq.toList with
                   | [] -> None
                   | l -> Some (l |> List.map(fun x -> x.GetValue<string>("key")))
        }

    type TweetsStorageActor(session: ISession) as this =
        inherit ReceiveActor()
        do
            this.ReceiveAsync<TweetsStorageMessage>(fun msg -> this.Handle(msg))
        member this.Handle(msg: TweetsStorageMessage) =
            let sender = this.Sender
            async {
                match msg with
                | Store tweets ->
                    session |> store(tweets) |> Async.Start
                | GetByKey key ->
                    let! res = session |> getByKey key
                    sender.Tell(res)
                | GetSearchKeys ->
                    let! res = session |> getSearchKeys
                    sender.Tell(res)
                return 0
            } |> Async.StartAsTask :> System.Threading.Tasks.Task



module TwitterApiClient =
    open SentimentFS.AnalysisServer.Domain.Tweets
    open SentimentFS.AnalysisServer.Domain.Sentiment
    open System
    open Akka.Actor
    open Tweetinvi
    open Tweetinvi.Models
    open Tweetinvi.Parameters

    let private spawn(credentials: ITwitterCredentials) =
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
                                        |> Seq.map(fun tweet -> { Id = Guid.NewGuid();
                                                                  IdStr = tweet.TweetDTO.IdStr;
                                                                  Text = tweet.TweetDTO.Text;
                                                                  Lang = tweet.TweetDTO.Language.ToString();
                                                                  Key = key; Date = tweet.TweetDTO.CreatedAt;
                                                                  Longitude = 0.0;
                                                                  Latitude = 0.0;
                                                                  Sentiment = Emotion.Neutral })
                                        |> Seq.toList
                        match result with
                        | [] -> reply.Reply(None)
                        | list -> reply.Reply(Some { value = list })
                    return! loop()
                }
            loop()
        )

    type TwitterApiActor(credentials: ITwitterCredentials) as this =
        inherit ReceiveActor()
        do
            this.ReceiveAsync<GetTweetsByKey>(fun msg -> this.Handle(msg))
        let agent = spawn(credentials)
        member this.Handle(msg: GetTweetsByKey) =
            let sender = this.Sender
            async {
                let! result = agent.PostAndAsyncReply(fun ch -> GetTweets(msg.key, ch))
                sender.Tell(result)
                return 0
            } |> Async.StartAsTask :> System.Threading.Tasks.Task


module TweetsMaster =
    open Cassandra
    open Akka.Actor
    open Tweetinvi.Models

    type TweetsMasterActor(session: ISession, credentials : ITwitterCredentials) =
        inherit ReceiveActor()


