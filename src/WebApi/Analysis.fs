namespace SentimentFS.AnalysisServer.WebApi

module Analysis =
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

    let analysisController(config: AppConfig, system: ActorSystem) =
        // let cluster =
        //     Cluster
        //         .Builder()
        //         .AddContactPoint("127.0.0.1")
        //         .WithDefaultKeyspace("sentiment_fs")
        //         .Build()

        // let session = cluster.ConnectAndCreateDefaultKeyspaceIfNotExists()

        let analysisActor =
            system.ActorOf(Props.Create<AnalysisActor>(), Actors.analysisActor.Name)

        let sentimentActor = createSentimentActor config.Sentiment.InitFileUrl system

        let getAnalysisResultByKey(key):WebPart =
            fun (x : HttpContext) ->
                async {
                    let! result = analysisActor.Ask<string>({ key = key }) |> Async.AwaitTask
                    return! OK result x
                }

        pathStarts "/api/analysis" >=> choose [
            GET >=> choose [ pathScan "/api/analysis/result/%s" getAnalysisResultByKey ]
        ]
