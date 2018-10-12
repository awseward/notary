#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/ASeward.MiscTools/lib/net471/ASeward.MiscTools.dll"

open ASeward.MiscTools.Versioning
open Fake

FakeTargetStubs.createVersionTargets Target getBuildParam ["src/Notary/AssemblyInfo.fs"]

let projects = !! "/**/*.fsproj"

Target "Build:Release" (fun _ ->
    projects
    |> MSBuildRelease null "Clean;Rebuild"
    |> Log "AppBuild-Output: "
)

let paketOutputDir = ".dist"

Target "Paket:Pack" (fun _ ->
    FileHelper.CleanDir paketOutputDir

    Paket.Pack <| fun p ->
        { p with
            OutputPath = paketOutputDir
        }
)

Target "Paket:Push" (fun _ ->
    Paket.Push <| fun p ->
        { p with
            ApiKey = environVar "BUGSNAG_NET_NUGET_API_KEY"
            WorkingDir = paketOutputDir
        }
)

"Paket:Pack" <== ["Build:Release"]
"Paket:Push" <== ["Paket:Pack"]

RunTargetOrDefault "Build:Release"
