namespace SentimentFS.AnalysisServer.Core.Tests

module Analysis =
    open Expecto
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Core.Tweets.Messages
    open SentimentFS.AnalysisServer.Core.Sentiment
    [<Tests>]
    let tests =
        testList "Analysis" [
            testList "Trend" [
                testCase "Trend increasing" <| fun _ ->
                    let subject =[1;2;3;4;5;6;7] |> Trend.rate
                    Expect.equal subject Trend.Increasing "Trend should be increasing"
                testCase "Trend stable" <| fun _ ->
                    let subject =[1;1;1;1;1;1;1] |> Trend.rate
                    Expect.equal subject Trend.Stable "Trend should be stable"
                testCase "Trend decreasing" <| fun _ ->
                    let subject =[7;6;5;4;3;2;1] |> Trend.rate
                    Expect.equal subject Trend.Decreasing "Trend should be decreasing"
            ]
            testList "Sentiment" [
                test "groupTweetsBySentiment" {
                    let subject = { value = [ { Tweet.Zero() with Sentiment = Emotion.Negative }; { Tweet.Zero() with Sentiment = Emotion.Positive } ] } |> Sentiment.groupTweetsBySentiment
                    Expect.equal subject ([struct (Emotion.Negative, 1); struct (Emotion.Positive, 1)]) "should"
                }
            ]
            testList "KeyWords" [
                test "getFrom" {
                    let subject = [| "trend"; "trend"; "should"; "should"; "be" |] |> KeyWords.getFrom
                    Expect.sequenceEqual subject [| struct ("trend", 2) |] """should equal [| struct ("trend", 2) |] because should is stopWord and be is to short"""
                }
            ]
        ]
