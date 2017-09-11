namespace SentimentFS.AnalysisServer.Domain

module Sentiment =
    open SentimentFS.NaiveBayes.Dto

    type Sentiment =
        | VeryNegative = -2
        | Negative = -1
        | Neutral = 0
        | Positive = 1
        | VeryPositive = 2

    type Message =
        | Classify of string * AsyncReplyChannel<ClassificationScore<Sentiment>>
        | Train of TrainingQuery<Sentiment>


    type ClassifyMessage = { key : string }
    type TrainMessage = { trainQuery : TrainingQuery<Sentiment> }

