namespace SentimentFS.AnalysisServer.SentimentService

open Akkling
open Akkling.Persistence
open Actor
open System

module Program =
    open Akka.Actor

    [<EntryPoint>]
    let main _ =
        let system = System.create "sentimentfs" <| (Configuration.load())
        let actor = spawn system "classifier" <| propsPersist (sentimentActor(Some defaultClassificatorConfig))
        printfn "%A" actor.Path
        printfn "Cluster Node Address %A" ((system :?> ExtendedActorSystem).Provider.DefaultAddress)
        Console.ReadKey() |> ignore
        0 // return an integer exit code
