namespace SentimentFS.AnalysisServer.Messages


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

    type SentimentMessage =
        | TrainEvent of Train
        | SentimentCommand of SentimentActorCommand

    [<CLIMutable>]
    type ClassifyResult = { text: string; score: Map<Emotion, float> }
