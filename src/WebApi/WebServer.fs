namespace SentimentFS.AnalysisServer


module WebServer =

    open System.IO
    open Suave
    open Suave.Logging
    open System.Net
    open Suave.Filters
    open Suave.Operators
    open Suave.RequestErrors
    let start port =
        startWebServer defaultConfig (Successful.OK "Hello World!")
