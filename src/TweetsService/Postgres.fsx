#r "../../packages/Dapper/lib/net451/Dapper.dll"

[<CLIMutable>]
type TweetDto = { IdStr: string
                  Text: string
                  CreationDate: DateTime
                  Lang: string
                  Longitude: double
                  Latitude: double
                  TwitterUser: string
                  Sentiment: Emotion }

let insertTweet (connectionString: string)(tweet: TweetDto) =
    async {
        use connection = new NpgsqlConnection(connectionString)
        do! connection.ExecuteAsync("sentimentfs.insert_tweet", tweet, commandType = System.Nullable<CommandType>(CommandType.StoredProcedure)) |> Async.AwaitTask |> Async.Ignore
    }

