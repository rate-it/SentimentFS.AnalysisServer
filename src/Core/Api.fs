namespace SentimentFS.AnalysisServer.Core.Api
open Akka.Actor
open Akka.Routing
open SentimentFS.AnalysisServer.Core.Config
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Sentiment
open Cassandra
open SentimentFS.AnalysisServer.Core.Tweets.TweetsMaster
open SentimentFS.AnalysisServer.Core.Tweets.Messages

type ApiActor(config: AppConfig, session: ISession) as this =
    inherit ReceiveActor()
    do
        this.Receive<TrainMessage>(this.HandleTrainQuery)
        this.Receive<ClassifyMessage>(this.HandleClassifyQuery)
        this.Receive<GetTweetsByKey>(this.HandleGetTweetsByKey)

    let mutable sentimentActor: IActorRef = null
    let mutable tweetsMasterActor: IActorRef = null

    override this.PreStart() =
            sentimentActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<SentimentActor>(Some defaultClassificatorConfig), Actors.sentimentActor.Name)
            initSentimentActor(config.Sentiment.InitFileUrl)(sentimentActor)
            tweetsMasterActor <- Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorOf(Props.Create<TweetsMasterActor>(session, config.TwitterApiCredentials).WithRouter(FromConfig.Instance), Actors.tweetsMaster.Name)
            base.PreStart()

    member this.HandleTrainQuery(msg: TrainMessage) =
        sentimentActor.Forward(msg)
        true

    member this.HandleClassifyQuery(msg: ClassifyMessage) =
        sentimentActor.Forward(msg)
        true

    member this.HandleGetTweetsByKey(msg: GetTweetsByKey) =
        tweetsMasterActor.Forward(msg)
        true
