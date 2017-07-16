// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake

let projects = !! "/**/*.fsproj"

Target "Build:Release" (fun _ ->
    projects
    |> MSBuildRelease null "Clean;Rebuild"
    |> Log "AppBuild-Output: "
)

RunTargetOrDefault "Build:Release"
