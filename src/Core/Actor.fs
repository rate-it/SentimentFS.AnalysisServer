namespace SentimentFS.AnalysisServer.Core.Actor

type ActorType = { Name: string; Parent: ActorType option; Path: string }
    with static member Create(name, parent) =
        let parentPath = match parent with
                         | Some p -> p.Path
                         | None -> "/user"
        { Name = name; Parent = parent; Path = (sprintf "%s/%s" parentPath name) }

module Actors =
    let tweetsMaster = ActorType.Create("tweets", None)
    let twitterApiActor = ActorType.Create("twitter-api", Some tweetsMaster)
    let tweetsDbActor = ActorType.Create("database", Some tweetsMaster)
