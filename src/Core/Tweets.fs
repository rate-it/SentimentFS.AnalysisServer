namespace SentimentFS.AnalysisServer.Core.Tweets

module Messages =
    open System
    open SentimentFS.AnalysisServer.Core.Sentiment
    open Cassandra
    open SentimentFS.NaiveBayes.Dto

    [<CLIMutable>]
    type Tweet = { Id: Guid
                   IdStr: string
                   Text: string
                   Key: string
                   Date: DateTime
                   Lang: string
                   Longitude: double
                   Latitude: double
                   Sentiment: Emotion } with
        static member FromCassandraRow(x: Row) = { Id = x.GetValue<Guid>("id")
                                                   IdStr = x.GetValue<string>("id_str")
                                                   Text = x.GetValue<string>("text")
                                                   Key = x.GetValue<string>("key")
                                                   Date = x.GetValue<DateTime>("date")
                                                   Lang = x.GetValue<string>("lang")
                                                   Longitude = x.GetValue<double>("longitude")
                                                   Latitude = x.GetValue<double>("latitude")
                                                   Sentiment = (LanguagePrimitives.EnumOfValue(x.GetValue<int>("sentiment"))) }
        static member Zero () = { Id = Guid.NewGuid()
                                  IdStr = ""
                                  Text = ""
                                  Key = ""
                                  Date = DateTime.Now
                                  Lang = ""
                                  Longitude = 0.0
                                  Latitude = 0.0
                                  Sentiment = Emotion.Neutral }
        member this.WithNewSentiment(score: ClassificationScore<Emotion>) =
            let bestEmotion, _ = score.score |> Map.toList |> List.maxBy(fun (emotion, value) -> value)
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


module TweetsStorage =
    open System
    open SentimentFS.AnalysisServer.Core
    open SentimentFS.AnalysisServer.Core.Sentiment
    open Messages
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
    open Messages
    open SentimentFS.AnalysisServer.Core.Sentiment
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
    open Messages
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.AnalysisServer.Core.Sentiment
    open TweetsStorage
    open TwitterApiClient
    open SentimentFS.NaiveBayes.Dto

    type TweetsMasterActor(session: ISession, credentials : ITwitterCredentials) as this =
        inherit ReceiveActor()

        let mutable tweetDbActor: IActorRef = null
        let mutable twitterApiActor: IActorRef = null

        do
            this.ReceiveAsync<GetTweetsByKey>(fun msg -> this.HandleGetTweetsByKey(msg))

        override this.PreStart() =
            tweetDbActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<TweetsStorageActor>(session), Actors.tweetStorageActor.Name)
            twitterApiActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<TwitterApiActor>(credentials), Actors.twitterApiActor.Name)
            base.PreStart()

        member this.HandleGetTweetsByKey(msg: GetTweetsByKey) =
            let sender = this.Sender
            let self = this.Self
            let sentimentActor = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorSelection(Actors.sentimentActor.Path)
            async {
                let! result = tweetDbActor.Ask<Tweets option>(GetByKey(msg.key)) |> Async.AwaitTask
                match result with
                | Some tweets ->
                    sender.Tell(tweets)
                | None ->
                    let! api = twitterApiActor.Ask<Tweets option>(msg) |> Async.AwaitTask
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
            } |> Async.StartAsTask :> System.Threading.Tasks.Task


