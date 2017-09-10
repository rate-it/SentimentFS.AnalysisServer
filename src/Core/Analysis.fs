namespace SentimentFS.AnalysisServer.Core.Analysis
open Akka.Actor
open SentimentFS.AnalysisServer.Domain.Analysis

type AnalysisActor() as this =
    inherit ReceiveActor()
    do this.Receive<GetAnalysisForKey>(fun msg -> printf "Elo: %s" (msg.key))
