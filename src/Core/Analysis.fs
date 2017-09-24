namespace SentimentFS.AnalysisServer.Core.Analysis
open Akka.Actor
open System
open SentimentFS.AnalysisServer.Core.Sentiment
open System.Collections.Generic

type Trend =
    | Increasing = 1
    | Stable = 0
    | Decreasing = -1

type AnalysisScore = { SentimentByQuantity: IDictionary<Emotion, int>
                       KeyWords: struct (string * int) seq
                       Localizations: struct (float32 * float32) seq
                       Key: string
                       Trend: Trend
                       DateByQuantity: IDictionary<DateTime, int> }
    with static member Zero key = { SentimentByQuantity = Dictionary<Emotion, int>()
                                    KeyWords = [||]
                                    Localizations = [||]
                                    Key = key
                                    Trend = Trend.Stable
                                    DateByQuantity = Dictionary<DateTime, int>() }

type GetAnalysisForKey = { key : string }


type AnalysisActor() as this =
    inherit ReceiveActor()
    do this.Receive<GetAnalysisForKey>(this.Handle)

    member this.Handle(msg: GetAnalysisForKey) =
        this.Sender.Tell(sprintf "Pozdro: %s" msg.key)
        true
