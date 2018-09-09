open Argu
open Notary
open Notary.CommandLine.Args
open System
open Notary.Shell

let private _nonzeroExit (parser: ArgumentParser<'a>) =
  parser.PrintUsage()
  |> printfn "%s"
  1

let private _detect (toolPaths: Tools.Paths) (args: ParseResults<DetectArgs>) =
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

let private _print (toolPaths: Tools.Paths) (args: ParseResults<PrintArgs>) =
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

let private _sign (toolPaths: Tools.Paths) (args: ParseResults<SignArgs>) =
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

[<EntryPoint>]
let main argv =
  let parser = ArgumentParser.Create<NotaryArgs>()
  try
    let parseResults = parser.Parse argv
    let toolPaths = getToolPaths parseResults

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
    | Some (Detect args) -> _detect toolPaths args
    | Some (Print args) -> _print toolPaths args
    | Some (Sign args) -> _sign toolPaths args
  with
  | Shell.MissingExecutableException filePath ->
      eprintfn "ERROR: Not found: %s" filePath
      eprintfn ""
      1
  | Shell.NonzeroExitException result ->
      result
      |> ProcessResult.PrintAllToStdErr
      |> fun r -> r.ExitCode
  | :? ArguParseException as ex ->
      eprintfn "%s" ex.Message
      eprintfn ""
      1
  | ex ->
      eprintfn "ERROR: %s" ex.Message
      eprintfn ""
      _nonzeroExit parser
