namespace SentimentFS.AnalysisServer.Core.Api
open Akka.Actor
open SentimentFS.AnalysisServer.Core.Config
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Sentiment

type ApiActor(config: AppConfig) as this =
    inherit ReceiveActor()
    do
        this.Receive<TrainMessage>(this.HandleTrainQuery)
        this.Receive<ClassifyMessage>(this.HandleClassifyQuery)

    let mutable sentimentActor: IActorRef = null

    override this.PreStart() =
            sentimentActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<SentimentActor>(Some defaultClassificatorConfig), Actors.sentimentActor.Name)
            initSentimentActor(config.Sentiment.InitFileUrl)(sentimentActor)
            base.PreStart()

    member this.HandleTrainQuery(msg: TrainMessage) =
        sentimentActor.Forward(msg)
        true

    member this.HandleClassifyQuery(msg: ClassifyMessage) =
        sentimentActor.Forward(msg)
        true
