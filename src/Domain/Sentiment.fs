namespace SentimentFS.AnalysisServer.Domain

module Sentiment =
    open SentimentFS.NaiveBayes.Dto

    type Emotion =
        | VeryNegative = -2
        | Negative = -1
        | Neutral = 0
        | Positive = 1
        | VeryPositive = 2

    type Message =
        | Classify of string * AsyncReplyChannel<ClassificationScore<Emotion>>
        | Train of TrainingQuery<Emotion>


    type ClassifyMessage = { text : string }
    type TrainMessage = { trainQuery : TrainingQuery<Emotion> }

