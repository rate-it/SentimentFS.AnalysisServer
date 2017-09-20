CREATE KEYSPACE IF NOT EXISTS sentiment
WITH replication = {
	'class' : 'SimpleStrategy',
	'replication_factor' : 1
};

use sentiment;
                          CREATE TABLE IF NOT EXISTS tweets (
                            Id uuid,
                            IdStr varchar,
                            Text text,
                            Key varchar,
                            Date timestamp,
                            Lang varchar,
                            Longitude double,
                            Latitude double,
                            Sentiment int,
                            PRIMARY KEY(Id)
                          );

INSERT INTO tweets (Id, IdStr, Text, Key, Date, Lang, Longitude, Latitude, Sentiment) VALUES (uuid(), 'da', 'dasdasdas', '', dateof(now()), '', 1.2, 3.1, 1);

SELECT * FROM tweets;
