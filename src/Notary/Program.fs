open Argu
open Notary
open Notary.CommandLine.Args
open System

// TODO: Unhardcode these
let private _certutil = @"C:\WINDOWS\System32\certutil.exe"
let private _signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"
let private _isFileSignedByPfx = Lib.isFileSignedByPfx _signtool _certutil
let private _getPfxCertHash = Lib.getPfxCertHash _certutil
let private _signIfNotSigned = Lib.signIfNotSigned _signtool _certutil

let _nonzeroExit (parser: ArgumentParser<'a>) =
    parser.PrintUsage()
    |> printfn "%s"
    1

let _subcommandNonzeroExit<'a when 'a :> IArgParserTemplate> () =
    ArgumentParser.Create<'a>()
    |> _nonzeroExit

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<NotaryArgs>()
    try
        let parseResults = parser.Parse argv
        match parseResults.TryGetSubCommand() with
        // It would be nice if these were actually just filtered from
        // TryGetSubCommand. I don't want to just add a catch-all,
        // because then I won't get warnings if I add an actual
        // subcommand without handling it here.
        | Some (Certutil _)
        | Some (Signtool _)
        | Some (Verbose)
        | Some (Quiet)
        | None ->
            _nonzeroExit parser

        | Some (Detect args) ->
            let pfx = args.GetResult <@ DetectArgs.Pfx @>
            let password = args.GetResult <@ DetectArgs.Password @>
            let file = args.GetResult <@ DetectArgs.File @>

            if _isFileSignedByPfx password pfx file then
                printfn "Already signed"
                0
            else
                printfn "Not signed"
                1

        | Some (Print args) ->
            let password = args.GetResult <@ PrintArgs.Password @>
            let pfx = args.GetResult <@ PrintArgs.Pfx @>

            pfx
            |> _getPfxCertHash password
            |> printfn "%s"
            0

        | Some (Sign args) ->
            let pfx = args.GetResult <@ SignArgs.Pfx @>
            let password = args.GetResult <@ SignArgs.Password @>
            let files = args.GetResult <@ SignArgs.Files @>

            files
            |> List.map (fun str -> str.Trim())
            |> _signIfNotSigned pfx password
            0
    with
    | Shell.NonzeroExitException exitCode -> exitCode
    | Lib.NotaryException ex ->
        printfn "ERROR: %s" ex.Message
        _nonzeroExit parser
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1