namespace SentimentFS.AnalysisServer


module WebServer =

    open System.IO
    open SentimentFS.AnalysisServer.WebApi.Analysis
    open Suave
    open Suave.Logging
    open System.Net
    open Suave.Filters
    open Suave.Operators
    open Suave.RequestErrors
    open Suave.Successful

    let app =
        choose [
            //analysisController()
        ]
    let start port =
        startWebServer defaultConfig app
