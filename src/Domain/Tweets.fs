namespace SentimentFS.AnalysisServer.Domain
open System
open MongoDB
open MongoDB.Bson.Serialization.Attributes

type Sentiment =
    | Negative = -1
    | Neutral = 0
    | Positive = 1

[<CLIMutable>]
type Tweet = { [<BsonId>] IdStr: string
               [<BsonElement("text")>] Text: string
               [<BsonElement("key")>] Key: string
               [<BsonElement("date")>] Date: DateTime
               [<BsonElement("lang")>] Lang: string
               [<BsonElement("longitude")>] Longitude: double
               [<BsonElement("latitude")>] Latitude: double
               [<BsonElement("sentiment")>] Sentiment: Sentiment }
