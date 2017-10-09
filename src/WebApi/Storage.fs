namespace SentimentFS.AnalysisServer.WebApi.Storage

module Cassandra =
    open Cassandra
    open SentimentFS.AnalysisServer.WebApi.Config

    let cluster (config: AppConfig) =
        let clusterBuilder =
            Cluster
                .Builder()
                .AddContactPoints(config.Cassandra.EndPoints)
                .WithDefaultKeyspace(config.Cassandra.KeyspaceName)

        if config.Cassandra.IsAuthenticated then
            clusterBuilder.WithCredentials(config.Cassandra.Username, config.Cassandra.Password) |> ignore

        clusterBuilder.Build()

    let session (config: AppConfig) (cluster: Cluster) =
        cluster.ConnectAndCreateDefaultKeyspaceIfNotExists(config.Cassandra.Replications)
