open System
#r "paket:
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MsBuild
nuget Fake.DotNet.Cli
nuget Fake.Core.Globbing"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet.Cli
open Fake.Core.Globbing.Operators

let solutionFile = "./SentimentFS.AnalysisServer.sln"
let dockerProjects = [ "./src/SentimentService/SentimentService.fsproj"; "./src/TweetsService/TweetsService.fsproj"; "./src/WebApi/WebApi.fsproj" ]
let dockerImages = ["sentimentfs.webapi"; "sentimentfs.sentimentservice" ]
// *** Define Targets ***
Target.Create "Clean" (fun _ ->
    !! "src/*/*/bin" |> Shell.CleanDirs
)

Target.Create "InstallDotnetCore" (fun _ ->
    DotnetCliInstall (fun x -> { x with Version = Version(GetDotNetSDKVersionFromGlobalJson()) })
)

Target.Create "DownloadPaket" (fun _ ->
    if 0 <> Process.ExecProcess (fun info ->
            { info with
                FileName = ".paket/paket.exe"
                Arguments = "--version" }
            ) (System.TimeSpan.FromMinutes 5.0) then
        failwith "paket failed to start"
)

Target.Create "Restore" (fun _ ->
    DotnetRestore (fun x -> x) solutionFile
)

Target.Create "Build" (fun _ ->
    DotnetBuild (fun x -> { x with Configuration = Release }) solutionFile
)

Target.Create "Publish" (fun _ ->
  for project in dockerProjects do
    DotnetPublish (fun x -> { x with Configuration = Release; OutputPath = Some "./obj/Docker/publish" }) project
)

Target.Create "CreateDockerImage" (fun _ ->
    for dockerImage in dockerImages do
        let result1 =
            Process.ExecProcessAndReturnMessages(fun (info:Process.ProcStartInfo) ->
                {info with
                    FileName = "docker"
                    Arguments = sprintf "ps -a -q | %% { docker rm %s }" dockerImage}) (System.TimeSpan.FromMinutes 15.)

        let result2 =
            Process.ExecProcessAndReturnMessages(fun (info:Process.ProcStartInfo) ->
                {info with
                    FileName = "docker"
                    Arguments = sprintf """images -q | %% { docker rmi %s  }""" dockerImage}) (System.TimeSpan.FromMinutes 15.)

        if result1.ExitCode <> 0  || result2.ExitCode <> 0 then failwith "RemoveOldDockerImages failed"
)


open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "InstallDotnetCore"
  ==> "DownloadPaket"
  ==> "Restore"
  ==> "Build"
  ==> "Publish"
  ==> "RemoveOldDockerImages"

// *** Start Build ***
Target.RunOrDefault "RemoveOldDockerImages"
