namespace SentimentFS.AnalysisServer.Core

module TweetsManager =
    let spawn =
        MailboxProcessor.Start(fun agent ->
            let rec messageLoop() = async{

                // read a message
                let! msg = agent.Receive()

                // process a message
                printfn "message is: %s" msg

                // loop to top
                return! messageLoop()
                }

    // start the loop
            messageLoop()
        )
