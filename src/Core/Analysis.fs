namespace SentimentFS.AnalysisServer.Core.Analysis

open Akka.Actor
open System
open SentimentFS.AnalysisServer.Core.Actor
open SentimentFS.AnalysisServer.Core.Sentiment
open SentimentFS.AnalysisServer.Core.Tweets.Messages
open SentimentFS.AnalysisServer.Core
open System.Collections.Generic

type Trend =
    | Increasing = 1
    | Stable = 0
    | Decreasing = -1

type AnalysisScore = { SentimentByQuantity: struct (Emotion * int) seq
                       KeyWords: struct (string * int) seq
                       Localizations: struct (double * double) seq
                       Key: string
                       Trend: Trend
                       DateByQuantity: IDictionary<DateTime, int> }
    with static member Zero key = { SentimentByQuantity = [||]
                                    KeyWords = [||]
                                    Localizations = [||]
                                    Key = key
                                    Trend = Trend.Stable
                                    DateByQuantity = Dictionary<DateTime, int>() }

type GetAnalysisForKey = { key : string }

module Trend =

    let private average(nums: float list) =
        let length = nums |> List.length |> double
        let sum = nums |> List.sum |> double
        sum / length

    let private a(nums: float list) =
        let y = nums
        let t = [0.0..(y |> List.length |> double) - 1.0]
        let averageT = t |> average
        let averageY = y |> average
        let tmta = t |> List.map(fun x -> x - averageT)
        let ymya = y |> List.map(fun x -> x - averageY)
        let numerator  = tmta |> List.zip ymya |> List.map (fun (a, b) -> a * b) |> List.fold ((+)) 0.0
        let denominator = tmta |> List.map(fun x -> x ** 2.0) |> List.fold((+)) 0.0
        numerator / denominator

    let rate (nums: int seq): Trend =
        let list = nums |> Seq.toList |> List.map(double)
        match list with
        | [] -> Trend.Stable
        | list ->
            let a = list |> a
            if Math.Abs(a) < Double.Epsilon then
                Trend.Stable
            else
                if a > 0.0 then Trend.Increasing else Trend.Decreasing


module KeyWords =
    open SentimentFS.TextUtilities
    let getFrom words =
        words
            |> Filter.filterOutSeq stopWords
            |> Seq.filter(fun x -> x.Length > 3)
            |> Seq.groupBy(id) |> Seq.map(fun (key, wordss) -> struct (key, wordss |> Seq.length))
            |> Seq.sortByDescending(fun struct (_, q) -> q)

module Sentiment =
    let groupTweetsBySentiment (tweets: Tweets): struct (Emotion * int) list =
        tweets.value |> List.groupBy(fun x -> x.Sentiment) |> List.map(fun (emotion, tweets) -> struct (emotion, tweets |> List.length))

module Localizations =
    let getFrom (tweets: Tweets) =
        tweets.value
                |> List.filter(fun x -> Math.Abs(x.Longitude) > Double.Epsilon || Math.Abs(x.Latitude) > Double.Epsilon)
                |> List.map(fun x -> struct (x.Longitude, x.Latitude))
                |> List.toSeq

type AnalysisActor() as this =
    inherit ReceiveActor()
    do this.ReceiveAsync<GetAnalysisForKey>(fun x -> this.HandleAsync(x))

    member private this.AnalyzeTweets(tweets: Tweets) : AnalysisScore = AnalysisScore.Zero("")

    member this.HandleAsync(msg: GetAnalysisForKey) =
        let sender = this.Sender
        let self = this.Self
        let tweetMaster = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current.ActorSelection(Actors.tweetsMaster.Path)
        async {
            let tweets: Tweets = { value = [ Tweet.Zero() ] }
            sender.Tell(sprintf "Pozdro: %s" msg.key)
        } |> Async.StartAsTask :> System.Threading.Tasks.Task
