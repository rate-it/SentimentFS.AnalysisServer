namespace SentimentFS.AnalysisServer.Domain.Tweets
open System
open SentimentFS.AnalysisServer.Domain.Sentiment
open Cassandra

[<CLIMutable>]
type Tweet = { Id: Guid
               IdStr: string
               Text: string
               Key: string
               Date: DateTime
               Lang: string
               Longitude: double
               Latitude: double
               Sentiment: Emotion }
    with static member FromCassandraRow(x: Row) = { Id = x.GetValue<Guid>("id")
                                                    IdStr = x.GetValue<string>("id_str")
                                                    Text = x.GetValue<string>("text")
                                                    Key = x.GetValue<string>("key")
                                                    Date = x.GetValue<DateTime>("date")
                                                    Lang = x.GetValue<string>("lang")
                                                    Longitude = x.GetValue<double>("longitude")
                                                    Latitude = x.GetValue<double>("latitude")
                                                    Sentiment = (LanguagePrimitives.EnumOfValue(x.GetValue<int>("sentiment"))) }


type Tweets = { value: Tweet list }
    with static member Empty = { value = [] }


type TweetsStorageMessage =
    | Store of Tweets
    | GetByKey of string
    | GetSearchKeys


type TwitterApiClientMessage =
    | GetTweets of key: string * AsyncReplyChannel<Tweets option>

type GetTweetsByKey = { key : string }
