open Argu
open Notary
open System

type DetectArgs =
| [<Mandatory>] Pfx of ``pfx filepath``:string
| [<MainCommand; ExactlyOnce; Last>] Files of ``files to check``:string list
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Pfx _ -> "pfx filepath"
            | Files _ -> "files which will be checked for having been signed by the given pfx file"
and PrintArgs =
| [<MainCommand; ExactlyOnce; Last>] Pfx of ``pfx filepath``:string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Pfx _ -> "pfx filepath"
and SignArgs =
| [<Mandatory>] Pfx of ``pfx filepath``:string
| [<Mandatory>] Password of string
| [<MainCommand; ExactlyOnce; Last>] Files of ``files to check``:string list
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Pfx _ -> "pfx filepath"
            | Password _ -> "password for pfx file"
            | Files _ -> "files which will be checked for having been signed by the given pfx file"
and NotaryArgs =
| [<CliPrefix(CliPrefix.None)>] Detect of ParseResults<DetectArgs>
| [<CliPrefix(CliPrefix.None)>] Print of ParseResults<PrintArgs>
| [<CliPrefix(CliPrefix.None)>] Sign of ParseResults<SignArgs>
| [<Inherit>] Certutil of path:string
| [<Inherit>] Signtool of path:string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Detect _ -> "detect whether a file is already signed by the specified pfx file"
            | Print _ -> "print the sha1 hash of a given pfx certificate file"
            | Sign _ -> "sign files with the given pfx certificate only if they're not already signed by that same certificate"
            | Certutil _ -> "certutil.exe filepath"
            | Signtool _ -> "signtool.exe filepath"

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
            printfn "Args: %A (Sign)" args
            0
        | _ ->
            printUsageAndExitOne()
    with
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1

    // // TODO: Put proper CLI parsing in place
    // if Array.isEmpty argv then _basicFail()
    // else
    //     // TODO: Unhardcode these
    //     let certutil = @"C:\WINDOWS\System32\certutil.exe"
    //     let signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"

    //     try
    //         match Array.head argv with
    //         | "ensureSigned" ->
    //             let pfx       = argv.[1]
    //             let password  = argv.[2]
    //             let filePaths =
    //                 argv
    //                 |> Array.skip 3
    //                 |> Array.map (fun str -> str.Trim())

    //             Lib.signIfNotSigned signtool certutil pfx password filePaths

    //             _block()
    //             0
    //         | _ ->
    //             _basicFail()
    //     with
    //     | ex ->
    //         printfn "%s" ex.Message
    //         _block()
    //         _basicFail()
