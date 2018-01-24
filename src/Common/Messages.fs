namespace SentimentFS.AnalysisServer.Common.Messages
open System

module Sentiment =

    type Emotion =
        | VeryNegative = -2
        | Negative = -1
        | Neutral = 0
        | Positive = 1
        | VeryPositive = 2

    [<CLIMutable>]
    type Classify = { text : string }

    [<CLIMutable>]
    type Train = { value: string; category: Emotion; weight : int option }

    type SentimentActorCommand =
        | Train of Train
        | Classify of Classify
        | GetState

    type SentimentMessage =
        | TrainEvent of Train
        | SentimentCommand of SentimentActorCommand

    type Sentiment = { emotion: Emotion; probability: float }

    type ClassificationResult = { text: string; score: Sentiment array }

    type ClassificatorState = { tokens: Map<string, int>; trainingsQuantity: int }

module TwitterApi =
    type GetTweets = { key: string }

module Twitter =
    open Sentiment

    type Tweet = { IdStr: string
                   Text: string
                   HashTags: string seq
                   CreationDate: DateTime
                   Language: string
                   Longitude: double
                   Latitude: double
                   Sentiment: Emotion }

    type Tweets = { tweets: Tweet seq }

    type SearchTweets = { key: string; since: DateTime; quantity: int }

    type TweetsCommands =
        | Add of Tweet
        | Search of key: string

    type TweetsMessage =
        | Init of tweets: Tweet seq
        | Add of Tweet


