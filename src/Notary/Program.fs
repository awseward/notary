open Argu
open Notary
open Notary.CommandLine.Args
open Notary.Shell

type ExitCodes =
| Zero = 0
| One = 1

let private _coerceFailure =
  function
  | NonzeroExit (exitCode, msg) -> (enum<ExitCodes> exitCode, Some msg)
  | ErrMsg msg -> (ExitCodes.One, Some msg)
  | ThrownExn ex -> (ExitCodes.One, Some ex.Message)

let private _detect (toolPaths: Tools.Paths) (args: ParseResults<DetectArgs>) =
  let pfx = args.GetResult <@ DetectArgs.Pfx @>
  let password = args.GetResult <@ DetectArgs.Password @>
  let file = args.GetResult <@ DetectArgs.File @>

  file
  |> Lib.isFileSignedByPfx
      toolPaths.signtool
      toolPaths.certutil
      password
      pfx
  |> Result.bind (fun isSigned ->
      if isSigned then Ok (Some "Already signed")
      else Error (ErrMsg "Not signed")
  )
  |> Result.mapError _coerceFailure

let private _print (toolPaths: Tools.Paths) (args: ParseResults<PrintArgs>) =
  let password = args.GetResult <@ PrintArgs.Password @>
  let pfx = args.GetResult <@ PrintArgs.Pfx @>

  pfx
  |> Lib.getPfxCertHash toolPaths.certutil password
  |> Result.map Some
  |> Result.mapError _coerceFailure

let private _sign (toolPaths: Tools.Paths) (args: ParseResults<SignArgs>) =
  let pfx = args.GetResult <@ SignArgs.Pfx @>
  let password = args.GetResult <@ SignArgs.Password @>
  let files = args.GetResult <@ SignArgs.Files @>

  files
  |> List.map (fun str -> str.Trim())
  |> Lib.signIfNotSigned
      toolPaths.signtool
      toolPaths.certutil
      pfx
      password
  |> Result.mapError _coerceFailure

let private _main argv (parser: ArgumentParser<NotaryArgs>) =
  try
    let parseResults = parser.Parse argv
    let toolPaths = getToolPaths parseResults

    match tryGetSubCommand parseResults with
    | Some (Detect args) -> _detect toolPaths args
    | Some (Print args) -> _print toolPaths args
    | Some (Sign args) -> _sign toolPaths args
    | _ ->
        parser.PrintUsage()
        |> fun msg -> ExitCodes.One, Some msg
        |> Error
  with
  | ex ->
      ex.Message
      |> fun str -> ExitCodes.One, Some str
      |> Error

[<EntryPoint>]
let main argv =
  ArgumentParser.Create<NotaryArgs>()
  |> _main argv
  |> function
      | Ok msg ->
          Option.iter (printfn "%s") msg
          0
      | Error (exitCode, msgOpt) ->
          Option.iter (eprintf "ERROR: %s") msgOpt
          int exitCode