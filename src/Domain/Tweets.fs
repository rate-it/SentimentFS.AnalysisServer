namespace SentimentFS.AnalysisServer.Domain

[<CLIMutable>]
type TweetDto = {  [<BsonId>]IdStr: string
                [<BsonElement("text")>] Text: string
                [<BsonElement("key")>] Key: string
                [<BsonElement("date")>] Date: DateTime
                [<BsonElement("lang")>] Lang: string
                [<BsonElement("longitude")>] Longitude: double
                [<BsonElement("latitude")>] Latitude: double
                [<BsonElement("sentiment")>] Sentiment: int }
