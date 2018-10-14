#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/ASeward.MiscTools/lib/net471/ASeward.MiscTools.dll"
#load "./temp/shims.fsx"

open ASeward.MiscTools
open ASeward.MiscTools.Versioning
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Notary.Fake.Shims

FakeTargetStubs.createVersionTargets Target Environment.environVar ["src/Notary/AssemblyInfo.fs"]

Target
  ReleaseNotes.FakeTargetStubs.targetName
  (fun _ ->
    ReleaseNotes.FakeTargetStubs.printReleaseNotes
      (Environment.environVarOrDefault)
      "awseward"
      "notary"
  )

let projects = !! "**/*.fsproj"

Target "Build:Release" (fun _ ->
    projects
    |> MSBuild.runRelease id null "Clean;Rebuild"
    |> Trace.logItems "AppBuild-Output: "
)

let paketOutputDir = ".dist"

Target "Paket:Pack" (fun _ ->
    Shell.cleanDir paketOutputDir

    Paket.pack <| fun p ->
        { p with
            OutputPath = paketOutputDir
        }
)

Target "Paket:Push" (fun _ ->
    Paket.push <| fun p ->
        { p with
            ApiKey = Environment.environVar "BUGSNAG_NET_NUGET_API_KEY"
            WorkingDir = paketOutputDir
        }
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"