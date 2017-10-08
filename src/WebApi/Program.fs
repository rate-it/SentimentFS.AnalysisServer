namespace SentimentFS.AnalysisServer.WebApi

open System
open Suave
open Akka.Actor
open Akka.Configuration
open SentimentFS.AnalysisServer
open SentimentFS.AnalysisServer.WebApi.Analysis
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Tweets.TwitterApiClient

module Program =
    open SentimentFS.NaiveBayes.Dto
    open System.IO
    open SentimentFS.AnalysisServer.Core.Tweets.Messages

    let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka.json"))
    let actorSystem =
            ActorSystem.Create("sentimentfs", akkaConfig)

    let sentimentActor =
            actorSystem.ActorOf(Props.Create<SentimentActor>(Some defaultClassificatorConfig), Actors.sentimentActor.Name)

    let GetEnvVar var =
        match System.Environment.GetEnvironmentVariable(var) with
        | null -> None
        | value -> Some value

    let getPortsOrDefault defaultVal =
        match System.Environment.GetEnvironmentVariable("APP_PORT") with
        | null -> defaultVal
        | value -> value |> uint16

    let twitterApiCredentialsFromEnviroment: TwitterCredentials =
        let consumerKey = GetEnvVar "CONSUMER_KEY"
        let consumerSecret = GetEnvVar "CONSUMER_SECRET"
        let accessToken = GetEnvVar "ACCESS_TOKEN"
        let accessTokenSecret = GetEnvVar "ACCESS_TOKEN_SECRET"
        match consumerKey, consumerSecret, accessToken, accessTokenSecret with
        | Some ck, Some cs, Some at, Some ats ->
            { ConsumerKey = ck; ConsumerSecret = cs; AccessToken = at; AccessTokenSecret = ats }
        | _ -> TwitterCredentials.Zero()

    let sentimentInitFileUrl: string =
        match GetEnvVar "SENTIMENT" with
        | Some x -> x
        | None ->  "https://raw.githubusercontent.com/wooorm/afinn-96/master/index.json"

    let twitterApiActor = actorSystem.ActorOf(Props.Create<TwitterApiActor>(twitterApiCredentialsFromEnviroment))

    [<EntryPoint>]
    let main argv =
        // try
        //     WebServer.start (getPortsOrDefault 8080us)
        //     0 // return an integer exit code
        // with
        // | ex ->
        //     let color = System.Console.ForegroundColor
        //     System.Console.ForegroundColor <- System.ConsoleColor.Red
        //     System.Console.WriteLine(ex.Message)
        //     System.Console.ForegroundColor <- color
        //     1
        (initSentimentActor(sentimentInitFileUrl) sentimentActor)
        printfn "%A" (sentimentActor.Ask<ClassificationScore<Emotion>>({ text = "My brother hate java" }) |> Async.AwaitTask |> Async.RunSynchronously)
        printfn "%A" (twitterApiActor.Ask<Tweets option>({ key = "fsharp" }) |> Async.AwaitTask |> Async.RunSynchronously)
        0
