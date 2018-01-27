namespace SentimentFS.AnalysisServer

module CassandraDb =
    open Cassandra

    let private createTweetsCollectionIfNotExists (session: ISession) =
        session.Execute("""
                          CREATE TABLE IF NOT EXISTS tweets (
                            id_str varchar,
                            text text,
                            key varchar,
                            date timestamp,
                            lang varchar,
                            longitude double,
                            latitude double,
                            sentiment int,
                            PRIMARY KEY(key, id_str)
                          );
                        """)
