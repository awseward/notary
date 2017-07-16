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

    module Lib =
        open System
        open System.Text.RegularExpressions
        open Shell

        let getPfxCertHash certutil pfx =
            let { stdOut = stdOut } = Shell.run certutil (sprintf "-dump %s" pfx)

            // This could definitely be loads better
            stdOut
            |> (fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries))
            |> Array.filter (fun str -> str.StartsWith("Cert Hash(sha1): "))
            |> Seq.last
            |> (fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", ""))
            |> (fun str -> str.Trim())
            |> (fun str -> str.Replace(" ", ""))
            |> (fun str -> str.ToUpperInvariant())

        let isFileSignedByCertHash signtool filePath certHash =
            let { proc = proc; stdOut = stdOut } =
                filePath
                |> sprintf "verify /v /all /pa /sha1 %s \"%s\"" certHash
                |> Shell.run signtool

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
            let (doNotNeedSigning, needSigning) =
                filePaths
                |> Array.ofSeq
                |> Array.partition (isFileSignedByPfx signtool certutil pfx)

            printfn "doNotNeedSigning: %A" doNotNeedSigning
            printfn "needSigning: %A" needSigning

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

            let { proc = proc; stdOut = stdOut; stdErr = stdErr } = Shell.run signtool args

            printfn "stdOut  : %s" stdOut
            printfn "stdErr  : %s" stdErr
            printfn "exitCode: %d" proc.ExitCode
