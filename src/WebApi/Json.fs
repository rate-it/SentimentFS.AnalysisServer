namespace SentimentFS.AnalysisServer.WebApi

module JSON =
    open Newtonsoft.Json

    let toJson value = JsonConvert.SerializeObject(value)

    let ofJson<'a>(json: string) = JsonConvert.DeserializeObject<'a>(json)

module SuaveJson =
    open JSON
    open Newtonsoft.Json
    open Suave
    open Suave.Successful
    open Suave.Operators

    let getResourceFromReq<'a> (req : HttpRequest) =
        let getString rawForm =
          System.Text.Encoding.UTF8.GetString(rawForm)
        req.rawForm |> getString |> ofJson<'a>

    let toJson v =
        v |> toJson |> OK >=> Writers.setMimeType "application/json; charset=utf-8"
