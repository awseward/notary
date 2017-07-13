namespace Notary

    module Lib =
        open System
        open System.Diagnostics
        open System.Text.RegularExpressions

        let extractCertHash (certUtilExeFilePath: string) (pfxFilePath: string) =
            let psi =
                ProcessStartInfo(
                    FileName = certUtilExeFilePath,
                    Arguments = sprintf "-dump %s" pfxFilePath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden)
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

        let isFileSignedByCertHash (filePath: string) (certHash: string) =
            // TODO
            false

        let isFileSignedByPfx (certUtilExeFilePath: string) (pfxFilePath: string) (filePath: string) =
            pfxFilePath
            |> extractCertHash certUtilExeFilePath
            |> isFileSignedByCertHash filePath