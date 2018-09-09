open Argu
open Notary
open Notary.CommandLine.Args
open System

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
        let toolPaths = Tools.pathsFromParseResults parseResults

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
            let isSigned =
              Lib.isFileSignedByPfx
                toolPaths.signtool
                toolPaths.certutil
                password
                pfx

            if isSigned file then
                printfn "Already signed"
                0
            else
                printfn "Not signed"
                1

        | Some (Print args) ->
            let password = args.GetResult <@ PrintArgs.Password @>
            let pfx = args.GetResult <@ PrintArgs.Pfx @>

            let getCertHash =
              Lib.getPfxCertHash
                toolPaths.certutil
                password

            pfx
            |> getCertHash
            |> printfn "%s"
            0

        | Some (Sign args) ->
            let pfx = args.GetResult <@ SignArgs.Pfx @>
            let password = args.GetResult <@ SignArgs.Password @>
            let files = args.GetResult <@ SignArgs.Files @>
            let signFiles =
              Lib.signIfNotSigned
                toolPaths.signtool
                toolPaths.certutil
                pfx
                password

            files
            |> List.map (fun str -> str.Trim())
            |> signFiles
            0
    with
    | Shell.NonzeroExitException exitCode -> exitCode
    | Lib.NotaryException ex ->
        printfn "ERROR: %s" ex.Message
        printfn ""
        _nonzeroExit parser
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1