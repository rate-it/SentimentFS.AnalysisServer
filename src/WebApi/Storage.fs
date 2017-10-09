namespace SentimentFS.AnalysisServer.WebApi.Storage

module Cassandra =
    open Cassandra
    open SentimentFS.AnalysisServer.WebApi.Config

    let cluster (config: AppConfig) =
