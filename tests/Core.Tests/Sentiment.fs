namespace SentimentFS.AnalysisServer.Core.Tests

module Sentiment =
    open Expecto
    open SentimentFS.AnalysisServer.Core
    open SentimentFS.AnalysisServer.Core.Sentiment
    open SentimentFS.NaiveBayes.Dto

    [<Tests>]
    let tests =
        testList "Sentiment" [
            testList "default Sync" [
                testCase "test when text is negative" <| fun _ ->
                    let positiveText = "I love fsharp"
                    let negativeText = "I hate java"
                    let classifier = Sentiment.spawn(Some Sentiment.defaultClassificatorConfig)
                    classifier.Post(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
                    classifier.Post(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
                    let subject = classifier.PostAndReply(fun ch -> Classify("My brother hate java", ch))
                    Expect.isGreaterThan (subject.score.TryFind(Emotion.Negative).Value) (subject.score.TryFind(Emotion.Positive).Value) "negative score should be greater than positive"
                testCase "test when text is positive" <| fun _ ->
                    let positiveText = "I love fsharp"
                    let negativeText = "I hate java"
                    let classifier = Sentiment.spawn(Some Sentiment.defaultClassificatorConfig)
                    classifier.Post(Train({ value = positiveText; category = Emotion.Positive; weight = None }))
                    classifier.Post(Train({ value = negativeText; category = Emotion.Negative; weight = None }))
                    let subject = classifier.PostAndReply(fun ch -> Classify("My brother love fsharp", ch))
                    Expect.isGreaterThan (subject.score.TryFind(Emotion.Positive).Value) (subject.score.TryFind(Emotion.Negative).Value) "positive score should be greater than negative"
            ]
        ]
