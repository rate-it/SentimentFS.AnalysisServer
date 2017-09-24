module Tests

open System
open Xunit
open Akka.TestKit.Xunit2
open Akka.Actor
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.NaiveBayes.Dto
type AnalysisActorTests() as this =
    inherit TestKit()

    [<Fact>]
    let ``Sentiment actor positive text`` () =
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        let actor = this.Sys.ActorOf(Props.Create<SentimentActor>(None))
        actor.Tell({ trainQuery =  { value = positiveText; category = Emotion.Positive; weight = None } })
        actor.Tell({ trainQuery =  { value = negativeText; category = Emotion.Negative; weight = None } })
        actor.Tell({ text = "My brother love fsharp" })
        let result = this.ExpectMsg<ClassificationScore<Emotion>>()
        Assert.True((result.score.TryFind(Emotion.Positive).Value) > (result.score.TryFind(Emotion.Negative).Value))

    [<Fact>]
    let ``Sentiment actor positive text`` () =
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        let actor = this.Sys.ActorOf(Props.Create<SentimentActor>(None))
        actor.Tell({ trainQuery =  { value = positiveText; category = Emotion.Positive; weight = None } })
        actor.Tell({ trainQuery =  { value = negativeText; category = Emotion.Negative; weight = None } })
        actor.Tell({ text = "My brother hate java" })
        let result = this.ExpectMsg<ClassificationScore<Emotion>>()
        Assert.True((result.score.TryFind(Emotion.Negative).Value) > (result.score.TryFind(Emotion.Positive).Value))
