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

            }
            loop()
        )
