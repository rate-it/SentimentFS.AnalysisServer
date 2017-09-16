namespace SentimentFS.AnalysisServer.Domain.Tweets
open System
open MongoDB
open MongoDB.Bson.Serialization.Attributes
open SentimentFS.AnalysisServer.Domain.Sentiment

[<CLIMutable>]
type Tweet = { [<BsonId>] IdStr: string
               [<BsonElement("text")>] Text: string
               [<BsonElement("key")>] Key: string
               [<BsonElement("date")>] Date: DateTime
               [<BsonElement("lang")>] Lang: string
               [<BsonElement("longitude")>] Longitude: double
               [<BsonElement("latitude")>] Latitude: double
               [<BsonElement("sentiment")>] Sentiment: Sentiment }


type Tweets = { value: Tweet list }
    with static member Empty = { value = [] }


type TweetsStorageMessage =
    | Store of Tweets
    | GetByKey of string
    | GetSearchKeys


type TwitterApiClientMessage =
    | GetTweets of key: string * AsyncReplyChannel<Tweets option>

type GetTweetsByKey = { key : string }
