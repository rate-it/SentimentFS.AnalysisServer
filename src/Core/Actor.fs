namespace SentimentFS.AnalysisServer.Core.Actor

type ActorType = { Name: string; Parent: ActorType option; Path: string }

module ActorType =
    let create(name, parent) =
        let parentPath = match parent with
                         | Some p -> p.Path
                         | None -> "/user"
        { Name = name; Parent = parent; Path = (sprintf "%s/%s" parentPath name) }

module Actors =
    open ActorType
    let analysisActor = create("analysis", None)
    let tweetsMaster = create("tweets", Some analysisActor)
    let twitterApiActor = create("twitter-api", Some tweetsMaster)
    let tweetStorageActor = create("storage", Some tweetsMaster)
    let sentimentActor = create("sentiment", Some tweetsMaster)
