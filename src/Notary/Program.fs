open Argu
open Notary
open Notary.CommandLine.Args
open System

type ToolPathOptions =
  {
    certUtilPath : string option
    signtoolPath : string option
  }
type ToolPaths =
  {
    certUtilPath : string
    signtoolPath : string
  }
let defaultToolPaths =
  {
    certUtilPath = @"C:\WINDOWS\System32\certutil.exe"
    signtoolPath =  @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"
  }
let private _asToolPathOptions toolPathOptions : ToolPathOptions =
  {
    certUtilPath = Some (toolPathOptions.certUtilPath)
    signtoolPath = Some (toolPathOptions.signtoolPath)
  }
let private _asToolPaths (toolPathOptions: ToolPathOptions) : ToolPaths =
  {
    certUtilPath = toolPathOptions.certUtilPath |> Option.defaultValue defaultToolPaths.certUtilPath
    signtoolPath = toolPathOptions.signtoolPath |> Option.defaultValue defaultToolPaths.signtoolPath
  }
let defaultToolPathOptions = _asToolPathOptions defaultToolPaths

let private _isFileSignedByPfx2 toolPathOptions =
  toolPathOptions
  |> _asToolPaths
  |> fun conf -> Lib.isFileSignedByPfx conf.signtoolPath conf.certUtilPath

let private _getPfxCertHash2 toolPathOptions =
  toolPathOptions
  |> _asToolPaths
  |> fun conf -> conf.certUtilPath
  |> Lib.getPfxCertHash

let private _signIfNotSigned2 toolPathOptions =
  toolPathOptions
  |> _asToolPaths
  |> fun conf -> Lib.signIfNotSigned conf.signtoolPath conf.certUtilPath

[<Obsolete("Prefer _isFileSignedByPfx2")>]
let private _isFileSignedByPfx = defaultToolPathOptions |> _isFileSignedByPfx2
[<Obsolete("Prefer _getPfxCertHash2")>]
let private _getPfxCertHash = defaultToolPathOptions |> _getPfxCertHash2
[<Obsolete("Prefer _signIfNotSigned2")>]
let private _signIfNotSigned = defaultToolPathOptions |> _signIfNotSigned2

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