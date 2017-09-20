namespace SentimentFS.AnalysisServer.Domain.Tweets
open System
open SentimentFS.AnalysisServer.Domain.Sentiment

[<CLIMutable>]
type Tweet = { IdStr: string
               Text: string
               Key: string
               Date: DateTime
               Lang: string
               Longitude: double
               Latitude: double
               Sentiment: Sentiment }


type Tweets = { value: Tweet list }
    with static member Empty = { value = [] }


type TweetsStorageMessage =
    | Store of Tweets
    | GetByKey of string
    | GetSearchKeys


type TwitterApiClientMessage =
    | GetTweets of key: string * AsyncReplyChannel<Tweets option>

type GetTweetsByKey = { key : string }
