namespace SentimentFS.AnalysisServer
open System
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open SentimentFS.AnalysisServer.Common.Messages.Twitter
open Nest

module Dto =

    [<CLIMutable>]
    type TweetDto = { IdStr: string
                      Text: string
                      CreationDate: DateTime
                      Lang: string
                      Longitude: double
                      Latitude: double
                      TwitterUser: string
                      Sentiment: Emotion } with
        static member FromTweet(x: Tweet) =
            { IdStr = x.IdStr
              Text = x.Text
              CreationDate = x.CreationDate
              Lang = x.Language
              Longitude = match x.Coordinates with | Some c -> c.Longitude | None -> 0.0
              Latitude = match x.Coordinates with | Some c -> c.Latitude | None -> 0.0
              TwitterUser = x.User
              Sentiment = defaultArg x.Sentiment Emotion.Neutral
            }
        static member ToTweet(x: TweetDto):Tweet =
            { IdStr = x.IdStr
              Text = x.Text
              CreationDate = x.CreationDate
              Language = x.Lang
              Coordinates = if x.Longitude = 0.0 && x.Latitude = 0.0 then None else Some { Longitude = x.Longitude; Latitude = x.Latitude }
              User = x.TwitterUser
              Sentiment = Some x.Sentiment
            }
        static member Zero () = { IdStr = ""
                                  Text = ""
                                  CreationDate = DateTime.Now
                                  Lang = ""
                                  Longitude = 0.0
                                  Latitude = 0.0
                                  TwitterUser = ""
                                  Sentiment = Emotion.Neutral }
open Dto
open System.Data

module Postgres =
    open Dapper
    open System.Data.Common

    let insertTweet (connection: #DbConnection)(tweet: TweetDto) =
        async {
            do! connection.ExecuteAsync("[sentimentfs].[insert_tweet]", tweet, commandType = System.Nullable<CommandType>(CommandType.StoredProcedure)) |> Async.AwaitTask |> Async.Ignore
        }

    let insertTweets (connection: #DbConnection)(tweets: TweetDto array) =
        async {
            do! connection.ExecuteAsync("[sentimentfs].[InserTweet]", tweets, commandType = System.Nullable<CommandType>(CommandType.StoredProcedure)) |> Async.AwaitTask |> Async.Ignore
        }

    let serachByKey(connection: #DbConnection)(key: string) =
        async {
            let args = dict ["Key", key]
            return! connection.QueryAsync<TweetDto>("[sentimentfs].[SearchTweetsByKey]", args, commandType = System.Nullable<CommandType>(CommandType.StoredProcedure)) |> Async.AwaitTask
        }


module Elastic =
    open Nest
    open Dto

    let indexName (name: string) =
        IndexName.op_Implicit name

    let tweetsIndexName = indexName "tweets"

    let indexer index (ides:IndexDescriptor<'T>)  =
        ides.Index(index) :> IIndexRequest<'T>

    let store (tweet: TweetDto)(client: ElasticClient) =
        async {
            return! client.IndexAsync(tweet, (fun idx -> indexer tweetsIndexName idx)) |> Async.AwaitTask
        }
