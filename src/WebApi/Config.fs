namespace SentimentFS.AnalysisServer.WebApi.Config
open System.Collections.Generic

[<CLIMutable>]
type CassandraConfig = { KeyspaceName: string; EndPoints: string[]; IsAuthenticated: bool; Username: string; Password: string; Replications: IDictionary<string, string> }

[<CLIMutable>]
type AppConfig = { Cassandra: CassandraConfig }
