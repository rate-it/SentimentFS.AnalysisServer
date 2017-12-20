namespace SentimentFS.AnalysisServer.SentimentService

open Akkling
open Akkling.Persistence
open Actor
open SentimentFS.NaiveBayes.Dto
open Akkling
open System

module Program =
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| Configuration.load()
        let remoteProps addr actor = { propsPersist actor with Deploy = Some (Deploy(RemoteScope(Address.Parse addr)));}
        let actor = spawn system "sentiment" <| (remoteProps "akka.tcp://sentimentfs@localhost:4500" (sentimentActor(Some defaultClassificatorConfig)))
        // actor <! SentimentCommand(Train({ trainQuery = { value = "I love fsharp"; category = Emotion.Positive; weight = None } }))
        // actor <! SentimentCommand(Train({ trainQuery = { value = "I hate java"; category = Emotion.Negative; weight = None } }))
        // async { let! reply = actor <? SentimentCommand(Classify({ text = "My brother love fsharp" }))
        //         printfn "Current state of %A: %A" actor reply } |> Async.RunSynchronously
        Console.ReadKey();
        0 // return an integer exit code
