namespace SentimentFS.AnalysisServer.WebApi

module Analysis =
    open Suave
    open Suave.Filters
    open Suave.Operators
    open Suave.Successful

    let analysisController() =
        pathStarts "/api/analysis" >=> choose [
            GET >=> choose [ pathScan "/api/analysis/result/%s"  (fun key -> OK(key)) ]
        ]
