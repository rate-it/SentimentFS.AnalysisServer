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

    type ClassifyResult = { text: string; score: Map<Emotion, float> }

    type ClassificatorState = { categories: Map<Emotion, Map<string, int>> }


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

    type TweetsMessage =
        | Init of tweets: Tweet seq * key: string
        | Add of Tweet


