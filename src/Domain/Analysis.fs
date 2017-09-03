namespace SentimentFS.AnalysisServer.Domain
open System
open System.Collections.Generic
open SentimentFS.AnalysisServer.Domain.Sentiment

type Trend =
    | Increasing = 1
    | Stable = 0
    | Decreasing = -1

type AnalysisScore = { SentimentByQuantity: IDictionary<Sentiment, int>
                       KeyWords: struct (string * int)
                       Localizations: struct (float32 * float32)
                       Key: string
                       Trend: Trend
                       DateByQuantity: IDictionary<DateTime, int> }
