namespace SentimentFS.AnalysisServer.Core.Api
open Akka.Actor
open SentimentFS.AnalysisServer.Core.Config

type ApiActor(config: AppConfig) =
    inherit ReceiveActor()

    member this.HandleTrainQuery() = 2
