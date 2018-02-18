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

    type Coordinates = { Longitude: double; Latitude: double }

    type TwitterUser = string

    type Tweet = { IdStr: string
                   Text: string
                   CreationDate: DateTime
                   Language: string
                   Coordinates: Coordinates option
                   User: TwitterUser
                   Sentiment: Emotion option }

    type Tweets = { tweets: Tweet seq }

    type SearchTweets = { key: string; since: DateTime option; quantity: int option }


    type TwitterApiActorMessage =
        | ApiSearch of SearchTweets

    type TweetsStorageActorMessage =
        | InsertOne of Tweet
        | InsertMany of Tweet list
        | Search of SearchTweets

    type TweetsActorMessage =
        | SearchByKey of key: string
        | Save of Tweet list

