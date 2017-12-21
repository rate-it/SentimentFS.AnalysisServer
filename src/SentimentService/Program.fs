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
        Console.ReadKey();
        0 // return an integer exit code
