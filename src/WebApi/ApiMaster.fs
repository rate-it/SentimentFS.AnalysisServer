namespace SentimentFS.AnalysisServer.WebApi.Api

module Actor =
    open Akka.Actor

    type ApiMasterActor() =
        inherit ReceiveActor()
