namespace SentimentFS.AnalysisServer.WebApi

module SentimentApi =
    open Akka.Actor
    open Operators
    open SentimentFS.AnalysisServer.Core.Actor
    open SentimentFS.NaiveBayes.Dto
    open SentimentFS.AnalysisServer.Core.Sentiment.Messages
    open SentimentFS.AnalysisServer.Core.Sentiment.Dto
    open JSON
    open Giraffe
    open Giraffe.Tasks
    open Giraffe.HttpHandlers
    open Giraffe.HttpContextExtensions
    open Microsoft.AspNetCore.Http


    let sentimentController (system: ActorSystem) =
        let classifyHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! model = ctx.BindModelAsync<Classify>()
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    let! result= api.Ask<ClassificationScore<Emotion>>(model)
                    return! customJson settings result next ctx
                }
        let trainHandler =
            fun (next : HttpFunc) (ctx : HttpContext) ->
                task {
                    let! model = ctx.BindModelAsync<TrainingQuery<Emotion>>()
                    let api = system.ActorSelection(Actors.apiActor.Path)
                    api.Tell({ trainQuery =  { value = model.value; category = model.category; weight = match model.weight with weight when weight > 1 -> Some weight | _ -> None } })
                    return! customJson settings "" next ctx
                }

        routeStartsWith  "/api/sentiment" >=> choose [
            POST >=> route "/api/sentiment/classification" >=> classifyHandler
            PUT >=> route "/api/sentiment/trainer" >=> trainHandler
        ]
