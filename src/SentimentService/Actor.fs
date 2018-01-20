namespace SentimentFS.AnalysisServer.SentimentService

module Actor =
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.TextUtilities
    open SentimentFS.Stemmer.Stemmer
    open Akka.Actor
    open Akkling
    open Akkling.Persistence
    open SentimentFS.NaiveBayes.Training
    open SentimentFS.NaiveBayes.Classification
    open System.Net.Http
    open Newtonsoft.Json
    open System.Collections.Generic

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

    let defaultClassificatorConfig: Config = { defaultWeight = 1; stem = stem; stopWords = stopWords }
    // categories: Map<Emotion, Map<string, int>>
    let sentimentActor config (mailbox: Actor<SentimentMessage>) =
        let rec loop (state) =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | TrainEvent trainMessage ->
                    return! loop (state |> Trainer.train({ value = trainMessage.value; category = trainMessage.category; weight = trainMessage.weight }))
                | SentimentCommand cmd ->
                    match cmd with
                    | Train train ->
                        return! Persist (TrainEvent(train))
                    | Classify query ->
                        let result = (state |> Classifier.classify(query.text)(Multinominal))
                        mailbox.Sender() <! { text = query.text; score = result.score }
                        return! loop state
                    | GetState ->
                        mailbox.Sender() <! { categories = (state.categories |> Map.map(fun _ x ->x.tokens ))  }
                        return! loop state
            }
        loop(ClassifierState.empty(Some config))
