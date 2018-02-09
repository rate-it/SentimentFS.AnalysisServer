namespace SentimentFS.AnalysisServer.WebApi

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe.Middleware
open Giraffe.Core
open Giraffe
open Newtonsoft.Json
open Giraffe.Serialization


module Program =
    open System.IO
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.Configuration

    let GetEnvVar var =
        match System.Environment.GetEnvironmentVariable(var) with
        | null -> None
        | value -> Some value

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

    let configuration =
        ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build()

    let configureCors (builder : CorsPolicyBuilder) =
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore

    let configureApp (app : IApplicationBuilder) =
        app.UseCors(configureCors)
           .UseGiraffeErrorHandler(errorHandler)
           .UseStaticFiles()
           .UseGiraffe(WebServer.app configuration)

    let configureServices (services : IServiceCollection) =
        services.AddGiraffe() |> ignore
        let jsonConverter = Fable.JsonConverter() :> JsonConverter
        let settings = JsonSerializerSettings(Converters = [|jsonConverter|])
        services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(settings)) |> ignore
        services.AddCors() |> ignore

    let configureLogging (builder : ILoggingBuilder) =
        let filter (l : LogLevel) = l.Equals LogLevel.Error
        builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

    [<EntryPoint>]
    let main argv =
        try
            let contentRoot = Directory.GetCurrentDirectory()
            let webRoot  = Path.Combine(contentRoot, "WebRoot")
            WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseIISIntegration()
                .UseWebRoot(webRoot)
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
                .Build()
                .Run()
            0 // return an integer exit code
        with
        | ex ->
            let color = System.Console.ForegroundColor
            System.Console.ForegroundColor <- System.ConsoleColor.Red
            System.Console.WriteLine(ex.Message)
            System.Console.ForegroundColor <- color
            1
