namespace SentimentFS.AnalysisServer.SentimentService

open Akkling
open Akkling.Persistence
open Actor
open SentimentFS.NaiveBayes.Dto
open Akkling
open System
open Akkling.Cluster.Sharding

module Program =
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| Configuration.load()
        let actor = ClusterSharding.entityFactoryFor system "sentiment" <| propsPersist (sentimentActor(Some defaultClassificatorConfig))
        Console.ReadKey() |> ignore
        0 // return an integer exit code
