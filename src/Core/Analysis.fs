namespace SentimentFS.AnalysisServer.Core.Analysis
open Akka.Actor
open SentimentFS.AnalysisServer.Domain.Analysis

type AnalysisActor() as this =
    inherit ReceiveActor()
    do this.Receive<GetAnalysisForKey>(this.Handle)

    member this.Handle(msg: GetAnalysisForKey) =
        this.Sender.Tell(sprintf "Pozdro: %s" msg.key)
        true
