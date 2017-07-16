open Argu
open Notary
open Notary.CommandLine.Args
open System

// TODO: Unhardcode these
let private _certutil = @"C:\WINDOWS\System32\certutil.exe"
let private _signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<NotaryArgs>()
    let printUsageAndExitOne () =
        parser.PrintUsage()
        |> printfn "%s"
        1

    try
        let parseResults = parser.Parse argv
        match parseResults.TryGetSubCommand() with
        | Some (Detect args) ->
            let maybePfx = args.TryGetResult <@ DetectArgs.Pfx @>
            let maybeFile = args.TryGetResult <@ DetectArgs.File @>

            match maybePfx, maybeFile with
            | Some pfx, Some file ->
                if Lib.isFileSignedByPfx _signtool _certutil pfx file then
                    printfn "Already signed"
                    0
                else
                    printfn "Not signed"
                    1
            | _ ->
                printUsageAndExitOne()

        | Some (Print args) ->
            match args.TryGetResult <@ PrintArgs.Pfx @> with
            | Some pfx ->
                pfx
                |> Lib.getPfxCertHash _certutil
                |> printfn "%s"
                0
            | None ->
                printUsageAndExitOne()

        | Some (Sign args) ->
            let maybePfx = args.TryGetResult <@ SignArgs.Pfx @>
            let maybePassword = args.TryGetResult <@ SignArgs.Password @>
            let maybeFiles = args.TryGetResult <@ SignArgs.Files @>

            match maybePfx, maybePassword, maybeFiles with
            | Some pfx, Some password, Some files ->
                files
                |> List.map (fun str -> str.Trim())
                |> Array.ofList // TODO: Fix this type mismatch
                |> Lib.signIfNotSigned _signtool _certutil pfx password

                0
            | _ ->
                printUsageAndExitOne()

        | Some (Certutil _)
        | Some (Signtool _)
        | None ->
            printUsageAndExitOne()
    with
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1