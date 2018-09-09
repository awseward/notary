namespace Notary

module Shell =
  open System
  open System.Diagnostics

  let private _present = (String.IsNullOrWhiteSpace >> not)
  let printfnIfAny str =
    if (_present str) then printfn "%s" str
  let eprintfnIfAny str =
    if (_present str) then eprintfn "%s" str

  type ProcessResult =
    {
      proc  : Process
      stdOut: string
      stdErr: string
    }
    with
      member this.FileName = this.proc.StartInfo.FileName
      member this.ExitCode = this.proc.ExitCode
      static member PrintStdOut result =
        printfnIfAny result.stdOut
        result
      static member PrintStdErr result =
        eprintfnIfAny result.stdErr
        result
      static member PrintAllToStdErr result =
        eprintfnIfAny result.stdOut
        ProcessResult.PrintStdErr result |> ignore
        eprintf "ERROR: %s terminated with exit code: %i" result.FileName result.ExitCode
        result

  exception NonzeroExitException of ProcessResult
  exception MissingExecutableException of string

  let createStartInfo filename arguments =
    ProcessStartInfo(
      FileName               = filename,
      Arguments              = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError  = true,
      UseShellExecute        = false,
      CreateNoWindow         = true,
      WindowStyle            = ProcessWindowStyle.Hidden)

  let runSync (startInfo: ProcessStartInfo) =
    try
      let proc = Process.Start(startInfo)
      let result =
        {
          proc = proc
          stdOut = proc.StandardOutput.ReadToEnd()
          stdErr = proc.StandardError.ReadToEnd()
        }
      proc.WaitForExit()
      result
    with
    | :? System.ComponentModel.Win32Exception as ex ->
        if ("The system cannot find the file specified" = ex.Message) then
          raise (MissingExecutableException startInfo.FileName)
        else
          reraise()

  let getCommandText (startInfo: ProcessStartInfo) =
    sprintf "%s %s" startInfo.FileName startInfo.Arguments

  let filterCommand filter startInfo =
    startInfo
    |> getCommandText
    |> filter

  let printCommandFiltered filter startInfo =
    startInfo
    |> filterCommand filter
    |> printfn "Notary: %s"

    startInfo

  let printCommand = printCommandFiltered id

  let ifZeroExit fn (result: ProcessResult) =
    if result.ExitCode = 0 then
      (fn result)
    else
      result

  let ifNonzeroExit fn (result: ProcessResult) =
    if result.ExitCode <> 0 then
      (fn result)
    else
      result

  let raiseIfExitNonzero =
    ifNonzeroExit (NonzeroExitException >> raise)