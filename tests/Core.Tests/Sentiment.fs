namespace SentimentFS.AnalysisServer.Core.Tests

module Sentiment =
    open Expecto
    open SentimentFS.AnalysisServer.Core
    open SentimentFS.AnalysisServer.Core.Sentiment
    open SentimentFS.NaiveBayes.Dto

    [<Tests>]
    let tests =
        testList "Sentiment" [
            testList "default Sync" []
        ]
