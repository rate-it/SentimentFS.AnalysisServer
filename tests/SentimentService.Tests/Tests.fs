namespace SentimentFS.AnalysisServer.SentimentService

module Tests =

    open System
    open Xunit
    open Akkling.TestKit
    open Akka.TestKit.Xunit2
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment
    open SentimentFS.AnalysisServer.SentimentService.Actor
    open Akkling.Persistence.Props
    open Akkling
    open Akkling.Behaviors

    [<Fact>]
    let ``My test`` () =
        Assert.True(true)

    [<Fact>]
    let ``Sentiment actor positive text`` () = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        actor <! SentimentCommand(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
        actor <! SentimentCommand(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
        actor <! SentimentCommand(Classify({ text = "My brother love fsharp" }))
        let a = expectMsg tck ({ text = "My brother love fsharp"; score = ([(Emotion.Negative, 0.0); (Emotion.Positive, 0.05555555556)] |> Map.ofList) })
        ()

        //Assert.True((result.score.TryFind(Emotion.Positive).Value) > (result.score.TryFind(Emotion.Negative).Value))
        // [<Fact>]
        // let ``Sentiment actor positive text`` () =
        //     let positiveText = "I love fsharp"
        //     let negativeText = "I hate java"
        //     let actor = this.Sys.ActorOf(Props.Create<SentimentActor>(None))
        //     actor.Tell({ trainQuery =  { value = positiveText; category = Emotion.Positive; weight = None } })
        //     actor.Tell({ trainQuery =  { value = negativeText; category = Emotion.Negative; weight = None } })
        //     actor.Tell({ text = "My brother hate java" })
        //     let result = this.ExpectMsg<ClassificationScore<Emotion>>()
        //     Assert.True((result.score.TryFind(Emotion.Negative).Value) > (result.score.TryFind(Emotion.Positive).Value))
