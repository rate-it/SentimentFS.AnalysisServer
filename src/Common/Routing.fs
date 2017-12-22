namespace SentimentFS.AnalysisServer.Common.Routing

type ActorMetaData  = { Name: string; Parent: ActorMetaData option; Path: string }

module ActorMetaData  =
    let create(name, parent) =
        let parentPath = match parent with
                         | Some p -> p.Path
                         | None -> "/user"
        { Name = name; Parent = parent; Path = (sprintf "%s/%s" parentPath name) }


module Actors =
    open ActorMetaData

    let api = create("api", None)
