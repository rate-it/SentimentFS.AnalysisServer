module Tests

open System
open Xunit
open Akka.TestKit.Xunit2

type AnalysisActorTests() =
    inherit TestKit()

    [<Fact>]
    let ``My test`` () =
        Assert.True(true)
