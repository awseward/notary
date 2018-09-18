namespace Notary

module Lib =
  open Shell
  open System
  open System.Text.RegularExpressions

  let getPfxCertHash certutil password pfx =
    let result =
      pfx
      |> Tools.Certutil.generateDumpArgs password
      |> Shell.buildStartInfo certutil
      |> Shell.printCommandFiltered Tools.Certutil.filterPassword
      |> Shell.runStartInfo

    match result with
    // TODO: Change this to no longer raise
    | (Error (NonzeroExit (exitCode, msg))) ->
        (exitCode, msg)
        |> TempNonzeroExitException
        |> raise
    | (Error (ThrownExn ex)) ->
        raise ex
    // ODOT
    | Ok stdOut ->
        stdOut
        |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun str -> str.StartsWith("Cert Hash(sha1): "))
        |> Seq.last
        |> fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", "")
        |> fun str -> str.Trim()
        |> fun str -> str.Replace(" ", "")
        |> fun str -> str.ToUpperInvariant()

  let partitionBySigned signtool certHash filePaths =
    let prefixText = "Successfully verified: "
    let signed =
      filePaths
      |> Tools.Signtool.generateVerifyArgs certHash
      |> Shell.buildStartInfo signtool
      |> Shell.printCommand
      |> Shell.runStartInfo
      |> function
          | Error (ThrownExn ex) ->
              raise ex
          | Error (NonzeroExit (_, output))
          | Ok output ->
              output
              |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
              |> Array.map (fun str -> str.Trim())
              |> Array.filter (fun str -> str.StartsWith prefixText)
              |> Array.map (fun str -> str.Replace(prefixText, ""))
              |> Set.ofArray

    signed
    |> Set.difference (Set.ofList filePaths)
    |> fun unsigned -> (Set.toArray signed, Set.toArray unsigned)

  let isFileSignedByPfx signtool certutil password pfx filePath =
    let certHash = getPfxCertHash certutil password pfx

    filePath
    |> List.singleton
    |> partitionBySigned signtool certHash
    |> fun (signed, unsigned) -> Seq.contains filePath signed && Seq.isEmpty unsigned

  let signIfNotSigned signtool certutil pfx password filePaths =
    let certHash = getPfxCertHash certutil password pfx
    let skipCount, filesToSign =
      filePaths
      |> List.ofSeq
      |> partitionBySigned signtool certHash
      |> fun (toSkip, toSign) -> (Array.length toSkip, Array.toList toSign)

    match skipCount with
    | 0 -> None
    | 1 -> Some ("file", "has")
    | _ -> Some ("files", "have")
    |> Option.iter (fun (fileOrFiles, hasOrHave) ->
        printfn
          "Skipping %d %s which %s already been signed with %s"
          skipCount
          fileOrFiles
          hasOrHave
          pfx
    )

    let any = not << List.isEmpty
    if any filesToSign then
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
      |> Shell.buildStartInfo signtool
      |> Shell.printCommandFiltered Tools.Signtool.filterPassword
      |> Shell.runStartInfo
      |> function
          | Ok stdOut -> printfn "%s" stdOut
          // TODO: Change this to no longer raise
          | Error (NonzeroExit (exitCode, msg)) ->
              (exitCode, msg)
              |> TempNonzeroExitException
              |> raise
          | Error (ThrownExn ex) ->
              raise ex
          // ODOT