namespace SentimentFS.AnalysisServer.WebApi

module SentimentApi =
    open Akka.Actor
    open Operators
    open Giraffe
    open Microsoft.AspNetCore.Http
    open SentimentFS.AnalysisServer.Common.Routing
    open SentimentFS.AnalysisServer.Common.Messages.Sentiment

    [<CLIMutable>]
    type TrainRequest = { text: string; category: Emotion; weight : int }

    let sentimentController (system: ActorSystem) =
        let classifyHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! model = ctx.BindModelAsync<Classify>()
                    let api = system.ActorSelection(Actors.router.Path)
                    let! result= api.Ask<ClassificationResult>(SentimentCommand(Classify(model)))
                    return! json result next ctx
                }

        let trainHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! model = ctx.BindModelAsync<TrainRequest>()
                    let api = system.ActorSelection(Actors.router.Path)
                    api.Tell(SentimentCommand(Train({ value = model.text; category = model.category; weight = match model.weight with weight when weight > 1 -> Some weight | _ -> None  })))
                    return! json "" next ctx
                }

        let getStateHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let api = system.ActorSelection(Actors.router.Path)
                    let! result = api.Ask<ClassificatorState>(SentimentCommand(GetState))
                    return! json result next ctx
                }

        routeStartsWith  "/api/sentiment" >=> choose [
            GET >=> route "/api/sentiment/state" >=> getStateHandler
            POST >=> route "/api/sentiment/classification" >=> classifyHandler
            PUT >=> route "/api/sentiment/trainer" >=> trainHandler
        ]
