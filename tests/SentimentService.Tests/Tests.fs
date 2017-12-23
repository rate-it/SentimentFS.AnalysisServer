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

    [<Fact>]
    let ``Sentiment actor only training``() = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        actor <! SentimentCommand(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
        actor <! SentimentCommand(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
        expectNoMsg tck

    [<Fact>]
    let ``Sentiment actor positive text`` () = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        actor <! SentimentCommand(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
        actor <! SentimentCommand(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
        actor <! SentimentCommand(Classify({ text = "My brother love fsharp" }))
        let result = tck.ExpectMsg<ClassifyResult>()
        Assert.True((result.score.TryFind(Emotion.Positive).Value) > (result.score.TryFind(Emotion.Negative).Value))

    [<Fact>]
    let ``Sentiment actor negative text`` () = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        actor <! SentimentCommand(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
        actor <! SentimentCommand(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
        actor <! SentimentCommand(Classify({ text = "My brother hate java" }))
        let result = tck.ExpectMsg<ClassifyResult>()
        Assert.True((result.score.TryFind(Emotion.Negative).Value) > (result.score.TryFind(Emotion.Positive).Value))

    [<Fact>]
    let ``Get state when actor has no any training`` () = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        actor <! SentimentCommand(GetState)
        let result = tck.ExpectMsg<ClassificatorState>()
        Assert.True(result.categories.IsEmpty)


    [<Fact>]
    let ``Get state when actor  training`` () = testDefault <| fun tck ->
        let actor = spawn tck "sentiment" (propsPersist (sentimentActor(None)))
        let positiveText = "I love fsharp"
        let negativeText = "I hate java"
        actor <! SentimentCommand(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
        actor <! SentimentCommand(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
        actor <! SentimentCommand(GetState)
        let result = tck.ExpectMsg<ClassificatorState>()
        Assert.False(result.categories.IsEmpty)
        Assert.True(result.categories.ContainsKey(Emotion.Positive))
        Assert.True(result.categories.ContainsKey(Emotion.Negative))
