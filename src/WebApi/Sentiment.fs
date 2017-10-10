namespace SentimentFS.AnalysisServer.WebApi

module Sentiment =
    open Akka.Actor
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful
    open SentimentFS.AnalysisServer.Core.Analysis
    open SentimentFS.AnalysisServer.Core.Sentiment
    open SentimentFS.AnalysisServer.Core.Actor
    open Cassandra
    open Tweetinvi
    open System.Net.Http
    open Newtonsoft.Json
    open SentimentFS.AnalysisServer.WebApi.Config
    open SentimentFS.NaiveBayes.Dto

    let intToEmotion (value: int): Emotion =
        match value with
        | -5 | -4 -> Emotion.VeryNegative
        | -3 | -2 | -1 -> Emotion.Negative
        | 0 -> Emotion.Neutral
        | 1 | 2 | 3 -> Emotion.Positive
        | 4 | 5 -> Emotion.VeryPositive
        | _ -> Emotion.Neutral

    let createSentimentActor (trainDataUrl: string) (system: ActorSystem) =
        let sentimentActor = system.ActorOf(Props.Create<SentimentActor>(), Actors.sentimentActor.Name)
        let httpResult = async {
            use client = new HttpClient()
            let! result = client.GetAsync(System.Uri(trainDataUrl)) |> Async.AwaitTask
            result.EnsureSuccessStatusCode() |> ignore
            return! result.Content.ReadAsStringAsync() |> Async.AwaitTask } |> Async.RunSynchronously

        let emotions = httpResult
                            |> JsonConvert.DeserializeObject<Map<string, int>>
                            |> Map.toList
                            |> List.map(fun (word, em) -> struct (word, em |> intToEmotion))

        for struct (word, emotion) in emotions do
            sentimentActor.Tell({ trainQuery =  { value = word; category = emotion; weight = None } })
        sentimentActor

    let sentimentController (trainDataUrl: string) (system: ActorSystem) =
        let sentimentActor = createSentimentActor trainDataUrl system
        let classify(text):WebPart =
            fun (x : HttpContext) ->
                async {
                    let! result = sentimentActor.Ask<ClassificationScore<Emotion>>({ text = text }) |> Async.AwaitTask
                    return! OK (result |> JSON.toJson) x
                }

        pathStarts "/api/sentiment" >=> choose [
            GET >=> choose [ pathScan "/api/sentiment/classification/%s" classify ]
        ]
