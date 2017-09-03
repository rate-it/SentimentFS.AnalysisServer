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


type TweetsManagerMessage =
    | Store of Tweet list
    | GetByKey of string * AsyncReplyChannel<Tweet list>


type Query =
    | GetByKey of string

type Command =
    | Create of tweet: Tweet
    | UpdateSentiment of newSentiment: Sentiment

type Event =
    | Created of tweet: Tweet
    | SentimentUpdated of newSentiment: Sentiment
