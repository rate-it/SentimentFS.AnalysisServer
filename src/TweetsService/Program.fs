// Learn more about F# at http://fsharp.org
namespace  SentimentFS.AnalysisServer.ActorService
open SentimentFS.AnalysisServer.Actor
open SentimentFS.AnalysisServer

module Program =
    open Akkling

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| (Configuration.load())
        let actorProps = tweetsActor((Storage.get InMemory))
        let actor = spawn system "classifier" <| props (actorProps)
        printfn "Hello World from F#!"
        0 // return an integer exit code
