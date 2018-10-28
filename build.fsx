#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/ASeward.MiscTools/lib/netstandard2.0/ASeward.MiscTools.dll"
#load "./temp/shims.fsx"

open ASeward.MiscTools
open ASeward.MiscTools.Versioning
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Notary.Fake.Shims

module CannedTargets =
  open ASeward.MiscTools.FakeTargets
  open ASeward.MiscTools.FakeTargets.Fake4

  let setup () =
    createVersionTargets Target getBuildParam ["src/Notary/AssemblyInfo.fs"]
    Target TargetNames.releaseNotesPrint <| fun _ -> releaseNotesPrint getBuildParamOrDefault "awseward" "notary"

CannedTargets.setup ()

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
            ApiKey = Environment.environVar "NUGET_API_KEY"
            WorkingDir = paketOutputDir
        }
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"
