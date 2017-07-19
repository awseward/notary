namespace Notary

module Shell =
    open System.Diagnostics

    type ProcessResult =
        {
            proc  : Process
            stdOut: string
            stdErr: string
        }

    let run filename arguments =
        let psi =
            ProcessStartInfo(
                FileName               = filename,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                WindowStyle            = ProcessWindowStyle.Hidden)

        let proc = Process.Start(psi)
        let stdOut = proc.StandardOutput.ReadToEnd()
        let stdErr = proc.StandardError.ReadToEnd()

        { proc = proc; stdOut = stdOut; stdErr = stdErr }

    let printCommand filterCommand (procResult: ProcessResult) =
        procResult
        |> (fun { proc = proc } -> proc.StartInfo)
        |> (fun startInfo -> sprintf "%s %s" startInfo.FileName startInfo.Arguments)
        |> (fun command ->
                match filterCommand with
                | Some fn -> fn command
                | None -> command)
        |> printfn "NOTARY: %s"

        procResult