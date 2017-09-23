namespace SentimentFS.AnalysisServer.Core

module Sentiment =
    open SentimentFS.NaiveBayes
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.NaiveBayes.Training
    open SentimentFS.NaiveBayes.Classification
    open SentimentFS.AnalysisServer.Domain.Sentiment
    open SentimentFS.Stemmer.Stemmer
    open SentimentFS.TextUtilities
    open Akka.Actor

    let private stopWords = """a about above after again against all am an and any are aren't as at be
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

    let spawn(config: Config option) =
        let trainer = Trainer.init<Emotion>(config)
        MailboxProcessor.Start(fun agent ->
                let rec loop (state: struct (State<Emotion> option * Config)) =
                    async {
                        let! msg = agent.Receive()
                        match msg with
                        | Classify(q, reply) ->
                            do state |> Classifier.classify(q) |> reply.Reply
                            return! state |> loop
                        | Train(query) ->
                            return! state |> Trainer.train(query) |> loop
                    }
                loop (trainer)
            )

    type SentimentActor(config: Config option) as this =
        inherit ReceiveActor()
        do
            this.Receive<TrainMessage>(this.HandleTrainMessage)
            this.ReceiveAsync<ClassifyMessage>(fun cm -> this.HandleClassifyMessage(cm))
        let agent = spawn(config)

        member this.HandleTrainMessage(msg: TrainMessage) : bool =
            agent.Post(Train(msg.trainQuery))
            true
        member this.HandleClassifyMessage(msg: ClassifyMessage) =
            let sender = this.Sender
            async {
                let! result = agent.PostAndAsyncReply(fun ch -> Classify(msg.text, ch))
                sender.Tell(result)
                return 0
            } |> Async.StartAsTask :> System.Threading.Tasks.Task
