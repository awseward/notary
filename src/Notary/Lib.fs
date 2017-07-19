namespace Notary

    module Shell =
        open System.Diagnostics

        type ProcessResult =
            {
                proc  : Process
                stdOut: string
                stdErr: string
            }
        with
            member this.Command =
                let startInfo = this.proc.StartInfo
                sprintf "%s %s" startInfo.FileName startInfo.Arguments

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

        let printCommand maybeFilter (procResult: ProcessResult) =
            let output =
                match maybeFilter with
                | Some fn -> fn procResult.Command
                | None -> procResult.Command
            printfn "NOTARY: %s" output
            procResult

    module Lib =
        open System
        open System.Text.RegularExpressions
        open Shell

        // NOTE: This is probably just a temporary crutch
        exception NotaryException of Exception

        let getPfxCertHash certutil pfx =
            let { stdOut = stdOut } =
                pfx
                |> sprintf "-dump %s"
                |> Shell.run certutil
                |> Shell.printCommand None

            // This could definitely be loads better
            try
                stdOut
                |> (fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries))
                |> Array.filter (fun str -> str.StartsWith("Cert Hash(sha1): "))
                |> Seq.last
                |> (fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", ""))
                |> (fun str -> str.Trim())
                |> (fun str -> str.Replace(" ", ""))
                |> (fun str -> str.ToUpperInvariant())
            with
            | ex -> raise (NotaryException ex)

        let isFileSignedByCertHash signtool filePath certHash =
            let { proc = proc; stdOut = stdOut } =
                filePath
                |> sprintf "verify /v /all /pa /sha1 %s \"%s\"" certHash
                |> Shell.run signtool
                |> Shell.printCommand None

            proc.ExitCode = 0 ||
                // This doesn't really feel great, but I don't see another way right now
                let prefixText = "number of signatures successfully verified:"
                stdOut
                |> (fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries))
                |> Array.map (fun str -> str.ToLowerInvariant())
                |> Array.find (fun str -> str.StartsWith(prefixText))
                |> (fun str -> str.Trim())
                |> (fun str -> str.Replace(prefixText, ""))
                |> int
                |> fun i -> i <> 0

        let isFileSignedByPfx signtool certutil pfx filePath =
            pfx
            |> getPfxCertHash certutil
            |> isFileSignedByCertHash signtool filePath

        let signIfNotSigned signtool certutil pfx password filePaths =
            let certHash = getPfxCertHash certutil pfx
            let (doNotNeedSigning, needSigning) =
                filePaths
                |> Array.ofSeq
                |> Array.partition (fun filePath -> isFileSignedByCertHash signtool filePath certHash)

            match Array.length doNotNeedSigning with
            | 0 -> ()
            | skipCount ->
                printfn "Skipping %d file(s) that have already been signed with %s" skipCount pfx

            if Array.isEmpty needSigning then
                ()
            else
                let timestampAlgo = "sha256"
                let timestampUrl  = "http://sha256timestamp.ws.symantec.com/sha256/timestamp"
                let filePathsAsSingleString =
                    needSigning
                    |> Array.map (sprintf "\"%s\"")
                    |> String.concat " "

                let args =
                    sprintf
                        "sign /v /as /td \"%s\" /tr \"%s\" /f \"%s\" /p \"%s\" %s"
                        timestampAlgo
                        timestampUrl
                        pfx
                        password
                        filePathsAsSingleString

                let { proc = proc; stdOut = stdOut; stdErr = stdErr } =
                    args
                    |> Shell.run signtool
                    |> Shell.printCommand (Some (fun str -> Regex.Replace(str, "/p [^ ]+ ", "/p [FILTERED] ")))

                printfn "stdOut  : %s" stdOut
                printfn "stdErr  : %s" stdErr
                printfn "exitCode: %d" proc.ExitCode
