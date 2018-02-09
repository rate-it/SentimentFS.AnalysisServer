namespace SentimentFS.AnalysisServer
open System
open SentimentFS.AnalysisServer.Common.Messages.Sentiment
open SentimentFS.AnalysisServer.Common.Messages.Twitter
open Nest

module Dto =

    [<CLIMutable>]
    type TweetDto = { IdStr: string
                      Text: string
                      HashTags: string seq
                      Date: DateTime
                      Lang: string
                      Longitude: double
                      Latitude: double
                      Sentiment: Emotion } with
        static member FromTweet(x: Tweet) =
            { IdStr = x.IdStr
              HashTags = x.HashTags
              Text = x.Text
              Date = x.CreationDate
              Lang = x.Language
              Longitude = match x.Coordinates with | Some c -> c.Longitude | None -> 0.0
              Latitude = match x.Coordinates with | Some c -> c.Latitude | None -> 0.0
              Sentiment = defaultArg x.Sentiment Emotion.Neutral
            }
        static member ToTweet(x: TweetDto):Tweet =
            { IdStr = x.IdStr
              HashTags = x.HashTags
              Text = x.Text
              CreationDate = x.Date
              Language = x.Lang
              Coordinates = if x.Longitude = 0.0 && x.Latitude = 0.0 then None else Some { Longitude = x.Longitude; Latitude = x.Latitude }
              Sentiment = Some x.Sentiment
            }
        static member Zero () = { IdStr = ""
                                  Text = ""
                                  HashTags = [|""|]
                                  Date = DateTime.Now
                                  Lang = ""
                                  Longitude = 0.0
                                  Latitude = 0.0
                                  Sentiment = Emotion.Neutral }
open Dto

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


type Storage =
    | InMemory


type ITweetsRepository =
    abstract member StoreAsync : TweetDto -> Async<Unit>
    abstract member GetAsync: SearchTweets -> Async<Tweet seq>


module Storage =
    type private InMemoryMessages =
    | Search of search: SearchTweets * AsyncReplyChannel<Tweet seq>
    | InsertTweets of TweetDto

    let private mailbox = lazy (
        MailboxProcessor.Start(fun inbox ->
            let rec loop tweets =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Search(s, reply) ->
                        let result = tweets |> List.filter(fun x -> x.Text.Contains(s.key)) |> List.map(TweetDto.ToTweet) |> List.toSeq
                        reply.Reply(result)
                        return! loop(tweets)
                    | InsertTweets dto ->
                        return! loop(dto :: tweets)

                }
            loop([])
        )
    )

    let get db =
        match db with
        | InMemory ->
            { new ITweetsRepository with
                member ___.StoreAsync(tweet) =
                    async {
                        mailbox.Value.Post(InsertTweets tweet)
                        return ()
                    }
                member ___.GetAsync(q) =
                    async {
                        return! mailbox.Value.PostAndAsyncReply(fun ch -> Search(q, ch))
                    }
            }
