namespace SentimentFS.AnalysisServer

open Common.Messages.Twitter
open Akka.Streams.Dsl
open Akkling.Streams
open Akka

module TwitterApi =

    let downloadTweetFlow (credentials: TwitterCredentials)= id

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



