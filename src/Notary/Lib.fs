namespace Notary

    module Lib =
        open System
        open System.Diagnostics
        open System.Text.RegularExpressions

        let getPfxCertHash (certutilExeFilePath: string) (pfxFilePath: string) =
            let psi =
                ProcessStartInfo(
                    FileName               = certutilExeFilePath,
                    Arguments              = sprintf "-dump %s" pfxFilePath,
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    WindowStyle            = ProcessWindowStyle.Hidden)
            let proc = Process.Start(psi)
            let stdOut = proc.StandardOutput.ReadToEnd()

            // This could definitely be loads better
            stdOut
            |> (fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries))
            |> Array.filter (fun (str: string) -> str.StartsWith("Cert Hash(sha1): "))
            |> Seq.last
            |> (fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", ""))
            |> (fun str -> str.Trim())
            |> (fun str -> str.Replace(" ", ""))
            |> (fun str -> str.ToUpperInvariant())

        let isFileSignedByCertHash (signtoolExeFilePath: string) (filePath: string) (certHash: string) =
            let psi =
                ProcessStartInfo(
                    FileName               = signtoolExeFilePath,
                    Arguments              = sprintf "verify /q /pa /sha1 %s \"%s\"" certHash filePath,
                    RedirectStandardOutput = false,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    WindowStyle            = ProcessWindowStyle.Hidden)
            let proc = Process.Start(psi)
            proc.WaitForExit()

            proc.ExitCode = 0

        let isFileSignedByPfx (signtoolExeFilePath: string) (certutilExeFilePath: string) (pfxFilePath: string) (filePath: string) =
            pfxFilePath
            |> getPfxCertHash certutilExeFilePath
            |> isFileSignedByCertHash signtoolExeFilePath filePath