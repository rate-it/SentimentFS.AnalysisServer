namespace SentimentFS.AnalysisServer.WebApi

open Akka.Actor
open Akka.Configuration
open SentimentFS.AnalysisServer.WebApi.Storage
open SentimentFS.AnalysisServer.Core.Actor
open System
open System.IO
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe.HttpHandlers
open Giraffe.Middleware


module Program =
    open System.IO
    open SentimentFS.AnalysisServer.Core.Config
    open SentimentFS.AnalysisServer.Core.Api
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

    let configureCors (builder : CorsPolicyBuilder) =
        builder.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader() |> ignore

    let configureApp (app : IApplicationBuilder) =
        app.UseCors(configureCors)
           .UseGiraffeErrorHandler(errorHandler)
           .UseStaticFiles()
           .UseGiraffe(WebServer.app)

    let configureServices (services : IServiceCollection) =
        let sp  = services.BuildServiceProvider()
        let env = sp.GetService<IHostingEnvironment>()
        services.AddCors() |> ignore

    let configureLogging (builder : ILoggingBuilder) =
        let filter (l : LogLevel) = l.Equals LogLevel.Error
        builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

    [<EntryPoint>]
    let main argv =
        let akkaConfig = ConfigurationFactory.ParseString(File.ReadAllText("./akka.json"))
        let configurationRoot = ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().AddCommandLine(argv).Build();
        let appconfig = AppConfig.Zero()
        configurationRoot.Bind(appconfig) |> ignore
        let actorSystem = ActorSystem.Create("sentimentfs", akkaConfig)
        let session = Cassandra.cluster appconfig |> Cassandra.session appconfig
        let apiActor = actorSystem.ActorOf(Props.Create<ApiMasterActor>(appconfig, session), Actors.apiActor.Name)

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
