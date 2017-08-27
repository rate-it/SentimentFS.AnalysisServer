namespace SentimentFS.AnalysisServer.Domain

module Sentiment =

    type Sentiment =
        | VeryNegative = -2
        | Negative = -1
        | Neutral = 0
        | Positive = 1
        | VeryPositive = 2
