namespace SentimentFS.AnalysisServer.WebApi

module JSON =
    open Newtonsoft.Json

    let jsonConverter = Fable.JsonConverter() :> JsonConverter

    let toJson value = JsonConvert.SerializeObject(value, [|jsonConverter|])

    let ofJson<'a>(json: string) = JsonConvert.DeserializeObject<'a>(json, [|jsonConverter|])

module SuaveJson =
    open JSON
    open Suave
    open Suave.Successful
    open Suave.Operators
    open Suave.Writers

    let getResourceFromReq<'a> (req : HttpRequest) =
        let getString rawForm =
          System.Text.Encoding.UTF8.GetString(rawForm)
        req.rawForm |> getString |> ofJson<'a>

    let toJson v =
        v
        |> toJson
        |> OK
        >=> setMimeType "application/json; charset=utf-8"
        >=> addHeader  "Access-Control-Allow-Origin" "*"
        >=> addHeader "Access-Control-Allow-Methods" "GET,POST,PUT"
