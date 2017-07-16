open Argu
open Notary
open Notary.CommandLine.Args
open System

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<NotaryArgs>()
    let printUsageAndExitOne () =
        parser.PrintUsage()
        |> printfn "%s"
        1
    // TODO: Unhardcode these
    let certutil = @"C:\WINDOWS\System32\certutil.exe"
    let signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"

    try
        let parseResults = parser.Parse argv
        match parseResults.TryGetSubCommand() with
        | Some (Detect args) ->
            // TODO: Double check that this case works ======================================================

            match args.TryGetResult <@ DetectArgs.Pfx @>, args.TryGetResult <@ DetectArgs.Files @> with
            | (Some pfx, Some files) ->
                // TODO: Fix to be a single file
                let file = Seq.head files

                file
                |> Lib.isFileSignedByPfx signtool certutil pfx
                |> printfn "%b"

                0
            | _ ->
                printUsageAndExitOne()

            // ----------------------------------------------------------------------------------------------
        | Some (Print args) ->
            match args.TryGetResult <@ PrintArgs.Pfx @> with
            | Some pfx ->
                pfx
                |> Lib.getPfxCertHash certutil
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
                |> Lib.signIfNotSigned signtool certutil pfx password

                0
            | _ ->
                printUsageAndExitOne()
        | _ ->
            printUsageAndExitOne()
    with
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1