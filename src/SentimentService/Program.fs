namespace SentimentFS.AnalysisServer.SentimentService

open Akkling
open Akkling.Persistence
open Actor
open SentimentFS.NaiveBayes.Dto
open Akkling
open System
open Akkling.Cluster.Sharding
open Akka.Cluster.Tools.Singleton
open System.Threading

module Program =
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| (Configuration.load())
        let actor = spawn system "classifier" <| propsPersist (sentimentActor(Some defaultClassificatorConfig))
        printfn "Cluster Node Address %A" ((system :?> ExtendedActorSystem).Provider.DefaultAddress)
        Console.ReadKey() |> ignore
        0 // return an integer exit code
