#r "../../packages/Dapper/lib/net451/Dapper.dll"
#r "../../packages/Npgsql/lib/net451/Npgsql.dll"
#r "../../packages/System.Threading.Tasks.Extensions/lib/portable-net45+win8+wp8+wpa81/System.Threading.Tasks.Extensions.dll"
open System 
open Dapper
open Npgsql
open System.Data

type Emotion =
    | VeryNegative = -2
    | Negative = -1
    | Neutral = 0
    | Positive = 1
    | VeryPositive = 2

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


async {
    do! insertTweet("User ID=postgres;Password=mysecretpassword;Server=127.0.0.1;Port=5432;Database=postgres;Pooling=true;")({ IdStr = ""
                                                                                                                               Text = ""
                                                                                                                               CreationDate = DateTime.Now
                                                                                                                               Lang = ""
                                                                                                                               Longitude = 0.0
                                                                                                                               Latitude = 0.0
                                                                                                                               TwitterUser = "" 
                                                                                                                               Sentiment = Emotion.Neutral })
                                                                                                                              
    return ()                                                                                                                                             
} |> Async.RunSynchronously