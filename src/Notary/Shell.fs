namespace Notary

open System

module Shell =
  open System
  open System.Diagnostics

  exception TempMissingExecutableException of string
  exception TempNonzeroExitException of int * string

  type Failure =
  | NonzeroExit of int * string
  | ThrownExn of Exception

  let private _shimMissingExecutableException filename =
    function
    | Error (ThrownExn ex) ->
        match ex with
        | :? System.ComponentModel.Win32Exception
          when "The system cannot find the file specified" = ex.Message ->
            TempMissingExecutableException filename
        | _ ->
            ex
        |> (Error << ThrownExn)
    | x -> x

  let private _buildNonzeroExit exitCode stdOut stdErr =
    [stdOut; stdErr]
    |> String.concat Environment.NewLine
    |> fun output -> exitCode, output
    |> (Error << NonzeroExit)

  let private _getResult (proc: Process) =
    let stdOut = proc.StandardOutput.ReadToEnd()
    let stdErr = proc.StandardError.ReadToEnd()
    proc.WaitForExit()

    if proc.ExitCode = 0 then
      Ok (stdOut)
    else
      _buildNonzeroExit
        proc.ExitCode
        stdOut
        stdErr

  let private _getResultAsync (proc: Process) =
    async {
      let! stdOut = proc.StandardOutput.ReadToEndAsync() |> Async.AwaitTask
      let! stdErr = proc.StandardError.ReadToEndAsync() |> Async.AwaitTask
      proc.WaitForExit()

      if proc.ExitCode = 0 then
        return Ok (stdOut)
      else
        return _buildNonzeroExit proc.ExitCode stdOut stdErr
    }

  let buildStartInfo filename arguments =
    ProcessStartInfo (
      FileName               = filename,
      Arguments              = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError  = true,
      UseShellExecute        = false,
      CreateNoWindow         = true,
      WindowStyle            = ProcessWindowStyle.Hidden
    )

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

  let runStartInfo (startInfo: ProcessStartInfo) =
    try
      use proc = Process.Start startInfo

      _getResult proc
    with
    | ex -> ex |> (Error << ThrownExn)

  let run filename arguments =
    try
      arguments
      |> buildStartInfo filename
      |> runStartInfo
    with
    | ex -> ex |> (Error << ThrownExn)

  let runStartInfoAsync (startInfo: ProcessStartInfo) =
    async {
      try
        use proc = Process.Start startInfo

        return! _getResultAsync proc
      with
      | ex -> return ex |> (Error << ThrownExn)
    }

  let runAsync filename arguments =
    async {
      try
        use proc =
          arguments
          |> buildStartInfo filename
          |> Process.Start

        return! _getResultAsync proc
      with
      | ex -> return ex |> (Error << ThrownExn)
    }

