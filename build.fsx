#r "paket:
nuget Fake.Core.Target prerelease
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MsBuild
nuget Fake.Core.Globbing
nuget Fake.Core.Target"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
// *** Define Targets ***
Target.Create "Clean" (fun _ ->
    Trace.log " --- Building the app --- "
)

Target.Create "Build" (fun _ ->
  Trace.log " --- Building the app --- "
)

Target.Create "Deploy" (fun _ ->
  Trace.log " --- Deploying app --- "
)


open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
Target.RunOrDefault "Deploy"
