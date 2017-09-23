namespace SentimentFS.AnalysisServer.Core.Tests

module Actor =
    open Expecto
    open SentimentFS.AnalysisServer.Core.Actor

    [<Tests>]
    let tests =
        testList "Actor" [
            testList "Actors" [
                test "TweetsMaster" {
                    let subject = Actors.tweetsMaster
                    Expect.equal subject.Name "tweets" "name should equal tweets"
                    Expect.isNone subject.Parent "Parent should be none"
                    Expect.equal subject.Path "/user/tweets" "path should equal tweets"
                }
                test "TweetsDbActor" {
                    let subject = Actors.tweetsDbActor
                    Expect.equal subject.Name "database" "name should equal database"
                    Expect.isSome subject.Parent "Parent should be Some"
                    Expect.equal subject.Path "/user/tweets/database" "path should equal tweets"
                }
                test "TwitterApiActor" {
                    let subject = Actors.twitterApiActor
                    Expect.equal subject.Name "twitter-api" "name should equal twitter-api"
                    Expect.isSome subject.Parent "Parent should be Some"
                    Expect.equal subject.Path "/user/tweets/twitter-api" "path should equal twitter-api"
                }
            ]
        ]
