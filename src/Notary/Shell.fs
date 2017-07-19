namespace Notary

module Shell =
    open System
    open System.Diagnostics

    type ProcessResult =
        {
            proc  : Process
            stdOut: string
            stdErr: string
        }

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
        let proc = Process.Start(startInfo)
        let result =
            {
                proc = proc
                stdOut = proc.StandardOutput.ReadToEnd()
                stdErr = proc.StandardError.ReadToEnd()
            }
        proc.WaitForExit()
        result

    let getCommandText (startInfo: ProcessStartInfo) =
        sprintf "%s %s" startInfo.FileName startInfo.Arguments

    let filterCommand filter startInfo =
        startInfo
        |> getCommandText
        |> (fun str ->
                match filter with
                | Some fn -> fn str
                | None -> str)

    let printFilteredCommand filter startInfo =
        startInfo
        |> filterCommand filter
        |> printfn "%s"

        startInfo

    let run filename arguments =
        arguments
        |> createStartInfo filename
        |> runSync

    [<Obsolete("Caller is probably better off printing command before it has already become a result")>]
    let printCommand filter (procResult: ProcessResult) =
        procResult
        |> (fun { proc = proc } -> proc.StartInfo)
        |> printFilteredCommand filter
        |> ignore

        procResult