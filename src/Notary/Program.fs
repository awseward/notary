open Argu
open Notary
open Notary.CommandLine.Args
open System
open Argu
open Notary

type Exit =
| Ok = 0
| Error = 1

let private _detect (toolPaths: Tools.Paths) (args: ParseResults<DetectArgs>) =
  let pfx = args.GetResult <@ DetectArgs.Pfx @>
  let password = args.GetResult <@ DetectArgs.Password @>
  let file = args.GetResult <@ DetectArgs.File @>

  file
  |> isFileSignedByPfx
      toolPaths.signtool
      toolPaths.certutil
      password
      pfx
  |> Shell.shimRaiseIfError
  |> fun isSigned ->
      if isSigned then
        printfn "Already signed"
        Exit.Ok
      else
        printfn "Not signed"
        Exit.Error

let private _print (toolPaths: Tools.Paths) (args: ParseResults<PrintArgs>) =
  let password = args.GetResult <@ PrintArgs.Password @>
  let pfx = args.GetResult <@ PrintArgs.Pfx @>

  pfx
  |> Lib.getPfxCertHash toolPaths.certutil password
  |> Shell.shimRaiseIfError
  |> printfn "%s"

  Exit.Ok

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

  Exit.Ok

let private _main argv (parser: ArgumentParser<NotaryArgs>) =
  try
    let parseResults = parser.Parse argv
    let toolPaths = getToolPaths parseResults

    match tryGetSubCommand parseResults with
    | Some (Detect args) -> _detect toolPaths args
    | Some (Print args) -> _print toolPaths args
    | Some (Sign args) -> _sign toolPaths args
    | _ ->
        parser.PrintUsage() |> eprintfn "%s"
        Exit.Error
  with
  | Shell.TempMissingExecutableException filePath ->
      eprintfn "ERROR: Not found: %s" filePath
      eprintfn ""
      Exit.Error
  | Shell.TempNonzeroExitException (exitCode, output) ->
      eprintfn "%s" output

      enum<Exit> exitCode
  | :? ArguParseException as ex ->
      eprintfn "%s" ex.Message
      Exit.Error
  | ex ->
      eprintfn "ERROR: %s" ex.Message
      Exit.Error

[<EntryPoint>]
let main argv =
  ArgumentParser.Create<NotaryArgs>()
  |> _main argv
  |> int