namespace SentimentFS.AnalysisServer.Core.Sentiment

open System.Collections.Generic
module Messages =
    open SentimentFS.NaiveBayes.Dto

    type Emotion =
        | VeryNegative = -2
        | Negative = -1
        | Neutral = 0
        | Positive = 1
        | VeryPositive = 2

    [<CLIMutable>]
    type Classify = { text : string }
    type Train = { trainQuery : TrainingQuery<Emotion> }

module Dto =
    [<CLIMutable>]
    type TrainingQuery<'a when 'a : comparison> = { value: string; category: 'a; weight : int }

module Actor =
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.TextUtilities
    open SentimentFS.Stemmer.Stemmer
    open SentimentFS.NaiveBayes.Training
    open SentimentFS.NaiveBayes.Classification
    open Akka.Actor
    open Messages
    open System.Net.Http
    open Newtonsoft.Json

    let private intToEmotion (value: int): Emotion =
        match value with
        | -5 | -4 -> Emotion.VeryNegative
        | -3 | -2 | -1 -> Emotion.Negative
        | 0 -> Emotion.Neutral
        | 1 | 2 | 3 -> Emotion.Positive
        | 4 | 5 -> Emotion.VeryPositive
        | _ -> Emotion.Neutral

    let stopWords = """a about above after again against all am an and any are aren't as at be
      because been before being below between both but by can't cannot could
      couldn't did didn't do does doesn't doing don't down during each few for from
      further had hadn't has hasn't have haven't having he he'd he'll he's her here
      here's hers herself him himself his how how's i i'd i'll i'm i've if in into
      is isn't it it's its itself let's me more most mustn't my myself no nor not of
      off on once only or other ought our ours ourselves out over own same shan't
      she she'd she'll she's should shouldn't so some such than that that's the
      their theirs them themselves then there there's these they they'd they'll
      they're they've this those through to too under until up very was wasn't we
      we'd we'll we're we've were weren't what what's when when's where where's
      which while who who's whom why why's with won't would wouldn't you you'd
      you'll you're you've your yours yourself yourselves""" |> Tokenizer.wordsSequence |> Seq.toList

    let defaultClassificatorConfig: Config = { model = Naive; defaultWeight = 1; stem = stem; stopWords = stopWords }

    type SentimentActor(tainingDataUrl: string, config: Config option) as this =
        inherit ReceiveActor()
        do
            this.Receive<Train>(this.HandleTrainMessage)
            this.Receive<Classify>(this.HandleClassifyMessage)
        let mutable state = Trainer.init<Emotion>(config)

        override this.PreStart() =
            let context =  Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current;
            let httpResult = async {
                use client = new HttpClient()
                let! result = client.GetAsync(System.Uri(tainingDataUrl)) |> Async.AwaitTask
                result.EnsureSuccessStatusCode() |> ignore
                return! result.Content.ReadAsStringAsync() |> Async.AwaitTask } |> Async.RunSynchronously

            let emotions = httpResult |> JsonConvert.DeserializeObject<IDictionary<string, int>>

            for keyValue in emotions do
                context.Self.Tell({ trainQuery =  { value = keyValue.Key; category = keyValue.Value |> intToEmotion ; weight = None } })

        member this.HandleTrainMessage(msg: Train) : bool =
            state <- state |> Trainer.train(msg.trainQuery)
            true

        member this.HandleClassifyMessage(msg: Classify) =
            state |> Classifier.classify(msg.text) |> this.Sender.Tell
            true
