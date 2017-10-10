namespace SentimentFS.AnalysisServer.WebApi

module JSON =
    open Newtonsoft.Json

    let toJson value = JsonConvert.SerializeObject(value)

    let ofJson<'a>(json: string) = JsonConvert.DeserializeObject<'a>(json)
