namespace Notary

module Lib =
  open Shell
  open System
  open System.Text.RegularExpressions

  let getPfxCertHash certutil password pfx =
    let { stdOut = stdOut } =
      pfx
      |> Tools.Certutil.generateDumpArgs password
      |> Shell.createStartInfo certutil
      |> Shell.printCommandFiltered Tools.Certutil.filterPassword
      |> Shell.runSync
      |> Shell.raiseIfExitNonzero

    stdOut
    |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.filter (fun str -> str.StartsWith("Cert Hash(sha1): "))
    |> Seq.last
    |> fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", "")
    |> fun str -> str.Trim()
    |> fun str -> str.Replace(" ", "")
    |> fun str -> str.ToUpperInvariant()

  let isFileSignedByCertHash signtool filePath certHash =
    let { proc = proc; stdOut = stdOut } =
      filePath
      |> Tools.Signtool.generateVerifyArgs certHash
      |> Shell.createStartInfo signtool
      |> Shell.printCommand
      |> Shell.runSync

    proc.ExitCode = 0 ||
      // This doesn't really feel great, but I don't see another way right now
      let prefixText = "number of signatures successfully verified:"
      stdOut
      |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
      |> Array.map (fun str -> str.ToLowerInvariant())
      |> Array.find (fun str -> str.StartsWith(prefixText))
      |> fun str -> str.Trim()
      |> fun str -> str.Replace(prefixText, "")
      |> int
      |> fun i -> i <> 0

  let isFileSignedByPfx signtool certutil password pfx filePath =
    pfx
    |> getPfxCertHash certutil password
    |> isFileSignedByCertHash signtool filePath

  let signIfNotSigned signtool certutil pfx password filePaths =
    let certHash = getPfxCertHash certutil password pfx
    let skipCount, filesToSign =
      filePaths
      |> Array.ofSeq
      |> Array.partition (fun filePath -> isFileSignedByCertHash signtool filePath certHash)
      |> fun (toSkip, toSign) ->
          (Array.length toSkip, List.ofArray toSign)

    if skipCount > 0 then
      printfn "Skipping %d file(s) that have already been signed with %s" skipCount pfx

    if List.isEmpty filesToSign then
      ()
    else
      let timestampAlgo = "sha256"
      let digestAlgo    = "sha256"
      let timestampUrl  = "http://sha256timestamp.ws.symantec.com/sha256/timestamp"
      let args =
        Tools.Signtool.generateSignArgs
          digestAlgo
          timestampAlgo
          timestampUrl
          pfx
          password
          filesToSign

      args
      |> Shell.createStartInfo signtool
      |> Shell.printCommandFiltered Tools.Signtool.filterPassword
      |> Shell.runSync
      |> Shell.ifExitZero ProcessResult.PrintStdOut
      |> Shell.raiseIfExitNonzero
      |> ignore
