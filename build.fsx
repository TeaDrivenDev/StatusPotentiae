System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

#r @"packages/build/FAKE/tools/FakeLib.dll"

open System.IO

open Fake

let solutionFile  = "StatusPotentiae.sln"

let outputDirectory = "bin"

Target "Clean" (fun _ -> CleanDirs [ outputDirectory ])

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuildRelease "" "Rebuild"
    |> ignore)

"Clean"
==> "Build"

RunTargetOrDefault "Build"