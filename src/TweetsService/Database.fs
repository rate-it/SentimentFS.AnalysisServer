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

module Postgres =
    open Dapper
    open Npgsql
    open Dto
    open System.Data

    let insertTweet (connectionString: string)(tweet: TweetDto) =
        async {
            use connection = new NpgsqlConnection(connectionString)
            do! connection.ExecuteAsync("INSERT INTO sentimentfs.tweets(idstr, text, creationdate, lang, longitude, latitude, twitteruser, sentiment) VALUES(@IdStr, @Text, @CreationDate, @Lang, @Longitude, @Latitude, @TwitterUser, @Sentiment);", tweet) |> Async.AwaitTask |> Async.Ignore
        }

    let insertTweets (connectionString: string)(tweets: TweetDto array) =
        async {
            use connection = new NpgsqlConnection(connectionString)
            do! connection.ExecuteAsync("INSERT INTO sentimentfs.tweets(idstr, text, creationdate, lang, longitude, latitude, twitteruser, sentiment) VALUES(@IdStr, @Text, @CreationDate, @Lang, @Longitude, @Latitude, @TwitterUser, @Sentiment);", tweets) |> Async.AwaitTask |> Async.Ignore
        }

    let serachByKey(connectionString: string)(key: string) =
        async {
            use connection = new NpgsqlConnection(connectionString)
            let args = DynamicParameters()
            args.Add("@Key", key)
            return! connection.QueryAsync<TweetDto>("""
                                                        SELECT
                                                          idstr,
                                                          text,
                                                          creationdate,
                                                          lang,
                                                          longitude,
                                                          latitude,
                                                          twitteruser,
                                                          sentiment
                                                        FROM sentimentfs.tweets WHERE text @@ to_tsquery(@Key);
                                                    """, args) |> Async.AwaitTask
        }


module Elastic =
    open Nest
    open Dto

    let indexName (name: string) =
        IndexName.op_Implicit name

    let typeName (name: string) =
        TypeName.op_Implicit name

    let tweetsIndexName = indexName "tweets"

    let tweetTypeName = typeName "tweet"

    let indexer index (ides:IndexDescriptor<'T>)  =
        ides.Index(index) :> IIndexRequest<'T>

    let insertTweet (tweet: TweetDto)(client: ElasticClient) =
        async {
            return! client.IndexAsync(tweet, (fun idx -> indexer tweetsIndexName idx)) |> Async.AwaitTask
        }


    let insertTweets (tweets: TweetDto array)(client: ElasticClient) =
        async {
            return! client.IndexManyAsync(tweets, tweetsIndexName, tweetTypeName) |> Async.AwaitTask
        }
