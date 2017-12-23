namespace SentimentFS.AnalysisServer.SentimentService

open Akkling
open Akkling.Persistence
open Actor
open SentimentFS.NaiveBayes.Dto
open Akkling
open System
open Akkling.Cluster.Sharding
open Akka.Cluster.Tools.Singleton

module Program =
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| (Configuration.load().WithFallback(ClusterSingletonManager.DefaultConfig()))
        let actor = spawn system "classifier" <| propsPersist (sentimentActor(Some defaultClassificatorConfig))
        printfn "cluster start"
        Console.ReadKey() |> ignore
        0 // return an integer exit code
