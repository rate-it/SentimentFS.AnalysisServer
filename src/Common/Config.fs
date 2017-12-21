namespace SentimentFS.AnalysisServer.Common.Config
open System.Collections.Generic

[<CLIMutable>]
type CassandraConfig = { KeyspaceName: string; EndPoints: string[]; IsAuthenticated: bool; Username: string; Password: string; Replications: Dictionary<string, string> }

[<CLIMutable>]
type Sentiment = { InitFileUrl: string }

[<CLIMutable>]
type AppConfig = { Cassandra: CassandraConfig; Sentiment: Sentiment; Port: uint16 }
with static member Zero() = { Cassandra = { KeyspaceName = ""; EndPoints = [||]; IsAuthenticated = false; Username = ""; Password = ""; Replications = Dictionary<string, string>() }
                              Sentiment = { InitFileUrl = "" }
                              Port = 8080us }
