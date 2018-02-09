// Learn more about F# at http://fsharp.org
namespace  SentimentFS.AnalysisServer.ActorService
open SentimentFS.AnalysisServer.Actor
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.Common.Routing
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open SentimentFS.AnalysisServer.Common.Messages.Twitter
open Akka.Routing
open System
open Argu

type ServiceConfig =
    | AccessToken of accessToken: string
    | AccessTokenSecret of accessTokenSecret: string
    | ConsumerKey of key: string
    | ConsumerSecret of secret: string
    | [<NoCommandLine>] PostgresConnectionString of conn: string
    | [<NoCommandLine>] ElasticSearchConnection of conn: string
with
    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | AccessToken _ -> "Twitter api access token"
            | AccessTokenSecret _ -> "Twitter api accessTokenSecret"
            | ConsumerKey _ -> "Twitter api consumer key"
            | ConsumerSecret _ -> "Twitter api consumer secret"
            | PostgresConnectionString _ -> "Postgres Connection String"
            | ElasticSearchConnection _ -> "ElasticSearch Connection"

module Program =
    open Akkling
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        let parser = ArgumentParser.Create<ServiceConfig>(programName = "gadget.exe")
        let system = System.create "sentimentfs" <| (Configuration.load())
        let db = Storage.get InMemory
        let actorProps = tweetsActor(db)
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
