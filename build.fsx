// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/FSharp.FakeTargets/tools/FSharp.FakeTargets.dll"

open Fake

let projects = !! "/**/*.fsproj"

Target "Build:Release" (fun _ ->
    projects
    |> MSBuildRelease null "Clean;Rebuild"
    |> Log "AppBuild-Output: "
)

Target "Package:NuGetFail" (fun _ ->
  @"

  Packaging with NuGet does not work. Please prefer Paket:* FAKE targets, or use paket.

  Paket usage:
    .paket/paket.exe pack .
    .paket/paket.exe push --api-key <API_KEY> <NUPKG_FILE>

  "
  |> failwith
)

datNET.Targets.initialize (fun p ->
    { p with
        AccessKey             = environVar "BUGSNAG_NET_NUGET_API_KEY"
        AssemblyInfoFilePaths = ["src/Notary/AssemblyInfo.fs"]
        Project               = "Notary"
        ProjectFilePath       = Some "src/Notary/Notary.fsproj"
        OutputPath            = "."
        WorkingDir            = "."
    }
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

// Deprecated
"Package:Project" <== ["Package:NuGetFail"; "Build:Release"]
"Publish" <== ["Package:Project"]

RunTargetOrDefault "Build:Release"
