namespace  SentimentFS.AnalysisServer

open Common.Messages.Twitter
module Actor =
    open Akkling
    open Akkling.Persistence
    open Akkling.Streams


    let tweetsActor (mailbox: Actor<TweetsMessage>) =
        let rec loop (state) =
            actor {
                let! msg = mailbox.Receive()
                return loop({tweets = []})
            }
        loop({tweets = []})



