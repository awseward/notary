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

datNET.Targets.initialize (fun p ->
    { p with
        AssemblyInfoFilePaths = ["src/Notary/AssemblyInfo.fs"]
        Project = "Notary"
        ProjectFilePath = Some "src/Notary/Notary.fsproj"
        OutputPath = "."
        WorkingDir = "."
    }
)

"Package:Project" <== ["Build:Release"]
"Publish" <== ["Package:Project"]

RunTargetOrDefault "Build:Release"
