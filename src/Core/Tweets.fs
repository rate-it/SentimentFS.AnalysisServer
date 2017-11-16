namespace SentimentFS.AnalysisServer.Core.Tweets

open System
module Messages =
    open System
    open Cassandra
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.AnalysisServer.Core.Sentiment.Messages

    [<CLIMutable>]
    type Tweet = { IdStr: string
                   Text: string
                   Key: string
                   Date: DateTime
                   Lang: string
                   Longitude: double
                   Latitude: double
                   Sentiment: Emotion } with
        static member FromCassandraRow(x: Row) = { IdStr = x.GetValue<string>("id_str")
                                                   Text = x.GetValue<string>("text")
                                                   Key = x.GetValue<string>("key")
                                                   Date = x.GetValue<DateTime>("date")
                                                   Lang = x.GetValue<string>("lang")
                                                   Longitude = x.GetValue<double>("longitude")
                                                   Latitude = x.GetValue<double>("latitude")
                                                   Sentiment = (enum<Emotion>(x.GetValue<int>("sentiment"))) }
        static member Zero () = { IdStr = ""
                                  Text = ""
                                  Key = ""
                                  Date = DateTime.Now
                                  Lang = ""
                                  Longitude = 0.0
                                  Latitude = 0.0
                                  Sentiment = Emotion.Neutral }
        member this.WithNewSentiment(score: ClassificationScore<Emotion>) =
            let bestEmotion, _ = score.score |> Map.toList |> List.maxBy(fun (_, value) -> value)
            { this with Sentiment = bestEmotion }



    type Tweets = { value: Tweet list }
        with static member Empty = { value = [] }


    type TweetsStorageMessage =
        | Store of Tweets
        | GetByKey of string
        | GetSearchKeys


    type TwitterApiClientMessage =
        | GetTweets of key: string * AsyncReplyChannel<Tweets option>

    type GetTweetsByKey = { key : string }
    type GetTweetsFromApi = { key: string; since: DateTime; quantity: int }
    type GetKeys = GetKeys

module TweetsStorage =
    open System
    open SentimentFS.AnalysisServer.Core.Sentiment
    open Messages
    open Cassandra
    open Akka.Actor

    let private createTweetsCollectionIfNotExists (session: ISession) =
        session.Execute("""
                          CREATE TABLE IF NOT EXISTS tweets (
                            id_str varchar,
                            text text,
                            key varchar,
                            date timestamp,
                            lang varchar,
                            longitude double,
                            latitude double,
                            sentiment int,
                            PRIMARY KEY(key, id_str)
                          );
                        """)

    let private store (tweets: Tweets) (session: ISession) =
        async {
            let batch = BatchStatement()
            let query = session.Prepare("""
                            INSERT INTO tweets (id_str, text, key, date, lang, longitude, latitude, sentiment) VALUES (?, ?, ?, ?, ?, ?, ?, ?);
                        """)
            for tweet in tweets.value do
                query.Bind(tweet.IdStr, tweet.Text, tweet.Key, tweet.Date, tweet.Lang, tweet.Longitude, tweet.Latitude, tweet.Sentiment |> int) |> batch.Add |> ignore

            return! batch |> session.ExecuteAsync |> Async.AwaitTask
        }

    let private getByKey (key:string) (session: ISession) =
        async {
            let! query = session.PrepareAsync("SELECT id_str, text, key, date, lang, longitude, latitude, sentiment FROM tweets WHERE key=? ALLOW FILTERING;") |> Async.AwaitTask
            let! result = session.ExecuteAsync(query.Bind(key)) |> Async.AwaitTask
            return result.GetRows() |> Seq.map(Tweet.FromCassandraRow)
        }

    let private getSearchKeys (session: ISession) =
        async {
            let q = SimpleStatement("SELECT DISTINCT key FROM tweets;")
            let! result = session.ExecuteAsync(q.SetPageSize(100)) |> Async.AwaitTask
            return result.GetRows() |> Seq.map(fun x -> x.GetValue<string>("key"))
        }

    type TweetsStorageActor(session: ISession) as this =
        inherit ReceiveActor()
        do
            createTweetsCollectionIfNotExists(session) |> ignore
            this.ReceiveAsync<TweetsStorageMessage>(fun msg -> this.Handle(msg))
        member this.Handle(msg: TweetsStorageMessage) =
            let sender = this.Sender
            async {
                match msg with
                | Store tweets ->
                    do! session |> store(tweets) |> Async.Ignore
                | GetByKey key ->
                    let! res = session |> getByKey key
                    match res |> Seq.toList with
                    | [] -> sender.Tell(None)
                    | list -> Some { value = list} |> sender.Tell
                | GetSearchKeys ->
                    let! res = session |> getSearchKeys
                    sender.Tell(res)
                return 0
            } |> Async.StartAsTask :> Threading.Tasks.Task



module TwitterApiClient =
    open Messages
    open SentimentFS.AnalysisServer.Core.Sentiment.Messages
    open Akka.Actor
    open Tweetinvi
    open Tweetinvi.Models
    open Tweetinvi.Parameters

    [<CLIMutable>]
    type TwitterCredentials = { ConsumerKey: string; ConsumerSecret: string; AccessToken: string; AccessTokenSecret: string }

    type TwitterApiActor(credentials: TwitterCredentials) as this =
        inherit ReceiveActor()
        do
            Auth.SetUserCredentials(credentials.ConsumerKey, credentials.ConsumerSecret, credentials.AccessToken, credentials.AccessTokenSecret) |> ignore
            this.ReceiveAsync<GetTweetsFromApi>(fun msg -> this.Handle(msg))

        member this.Handle(msg: GetTweetsFromApi) =
            let sender = this.Sender
            async {
                let options = SearchTweetsParameters(msg.key)
                options.SearchType <- Nullable<SearchResultType>(SearchResultType.Recent)
                options.Lang <- Nullable<LanguageFilter>(LanguageFilter.English)
                options.Filters <- TweetSearchFilters.None
                options.MaximumNumberOfResults <- msg.quantity
                options.Since <- msg.since
                let! queryResult = SearchAsync.SearchTweets(options) |> Async.AwaitTask
                let result = queryResult
                                |> Seq.filter(fun tweet -> not tweet.IsRetweet)
                                |> Seq.map(fun tweet -> { IdStr = tweet.IdStr;
                                                          Text = tweet.Text;
                                                          Lang = tweet.Language.ToString();
                                                          Key = msg.key;
                                                          Date = tweet.CreatedAt;
                                                          Longitude = match tweet.Coordinates with null -> 0.0 | coord -> coord.Longitude;
                                                          Latitude = match tweet.Coordinates with null -> 0.0 | coord -> coord.Latitude;
                                                          Sentiment = Emotion.Neutral })
                match result |> Seq.toList with
                | [] -> sender.Tell(None)
                | list -> Some { value = list} |> sender.Tell
            } |> Async.StartAsTask :> Threading.Tasks.Task


module TweetsMaster =
    open Cassandra
    open Akka.Actor
    open Messages
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.AnalysisServer.Core.Sentiment.Messages
    open TweetsStorage
    open TwitterApiClient
    open SentimentFS.NaiveBayes.Dto

    type TweetsMasterActor(session: ISession, credentials : TwitterCredentials) as this =
        inherit ReceiveActor()

        let mutable tweetDbActor: IActorRef = null
        let mutable twitterApiActor: IActorRef = null

        do
            this.ReceiveAsync<GetTweetsByKey>(fun msg -> this.HandleGetTweetsByKey(msg))
            this.Receive<GetKeys>(this.HandleGetKeys)

        override this.PreStart() =
            tweetDbActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<TweetsStorageActor>(session), Actors.tweetStorageActor.Name)
            twitterApiActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<TwitterApiActor>(credentials), Actors.twitterApiActor.Name)
            base.PreStart()

        member this.HandleGetTweetsByKey(msg: GetTweetsByKey) =
            let sender = this.Sender
            let sentimentActor = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorSelection(Actors.sentimentActor.Path)
            async {
                let! result = tweetDbActor.Ask<Tweets option>(GetByKey(msg.key)) |> Async.AwaitTask
                match result with
                | Some tweets ->
                    sender.Tell(Some tweets)
                | None ->
                    let! api = twitterApiActor.Ask<Tweets option>({ key = msg.key; since = DateTime.MinValue; quantity = 1000 }) |> Async.AwaitTask
                    match api with
                    | Some apiTweets ->
                        let sentiments = apiTweets.value
                                            |> List.map ((fun tweet -> async {
                                                                                let! res = sentimentActor.Ask<ClassificationScore<Emotion>>({ text = tweet.Text }) |> Async.AwaitTask
                                                                                sentimentActor.Tell({ trainQuery =  { value = tweet.Text; category = Emotion.Positive; weight = None } })
                                                                                return tweet.WithNewSentiment(res)
                                                                             })) |> Async.Parallel |> Async.RunSynchronously
                        let tweetsList = sentiments |> Array.toList
                        tweetDbActor.Tell(Store({ value = tweetsList}))
                        sender.Tell(Some { value = tweetsList})
                    | None ->
                        sender.Tell(None)
            } |> Async.StartAsTask :> Threading.Tasks.Task

        member this.HandleGetKeys(_: GetKeys) =
            tweetDbActor.Forward(GetSearchKeys)
            true

