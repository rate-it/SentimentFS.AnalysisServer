namespace SentimentFS.AnalysisServer.Core.Tests

module Program =
    open System
    open Expecto

    [<EntryPoint>]
    let main argv =
        Tests.runTestsInAssembly defaultConfig argv
