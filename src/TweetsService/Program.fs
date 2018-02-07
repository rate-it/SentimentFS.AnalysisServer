// Learn more about F# at http://fsharp.org
namespace  SentimentFS.AnalysisServer.ActorService
open SentimentFS.AnalysisServer.Actor
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.Common.Routing
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open SentimentFS.AnalysisServer.Common.Messages.Twitter
open Akka.Routing
open System

module Program =
    open Akkling
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let system = System.create "sentimentfs" <| (Configuration.load())
        let actorProps = tweetsActor((Storage.get InMemory))
        let sentimentactor = spawn system Actors.sentimentRouter.Name <| Props<SentimentMessage>.From(Props.Empty.WithRouter(FromConfig.Instance))
        //let twitterApiActor = spawn system Actors.twitterApiActor.Name props()
        let actor = spawn system Actors.tweetsActor.Name <| props (actorProps)

        async {
            let! res = actor <? Search { key = "dupa"; since = DateTime.Now; quantity = 100  }
            printfn "%A" res
            return ()
        } |> Async.RunSynchronously
        printfn "Hello World from F#!"
        Console.ReadKey()
        0 // return an integer exit code
