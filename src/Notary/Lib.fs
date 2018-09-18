namespace Notary

module Lib =
  open Shell
  open System
  open System.Text.RegularExpressions

  let getPfxCertHash certutil password pfx =
    pfx
    |> Tools.Certutil.generateDumpArgs password
    |> Shell.buildStartInfo certutil
    |> Shell.printCommandFiltered Tools.Certutil.filterPassword
    |> Shell.runStartInfo
    |> Result.map (fun stdOut ->
        stdOut
        |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun str -> str.StartsWith("Cert Hash(sha1): "))
        |> Seq.last
        |> fun str -> Regex.Replace(str, "Cert Hash\(sha1\): ", "")
        |> fun str -> str.Trim()
        |> fun str -> str.Replace(" ", "")
        |> fun str -> str.ToUpperInvariant()
    )

  let partitionBySigned signtool certHash filePaths =
    let prefixText = "Successfully verified: "
    filePaths
    |> Tools.Signtool.generateVerifyArgs certHash
    |> Shell.buildStartInfo signtool
    |> Shell.printCommand
    |> Shell.runStartInfo
    |> function
        | Error (NonzeroExit (_, output))
        | Ok output ->
            output
            |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun str -> str.Trim())
            |> Array.filter (fun str -> str.StartsWith prefixText)
            |> Array.map (fun str -> str.Replace(prefixText, ""))
            |> Set.ofArray
            |> Ok
        | Error x -> Error x
    |> Result.map (fun signed ->
        signed
        |> Set.difference (Set.ofList filePaths)
        |> fun unsigned -> (Set.toArray signed, Set.toArray unsigned)
    )

  let isFileSignedByPfx signtool certutil password pfx filePath =
    pfx
    |> getPfxCertHash certutil password
    |> Result.bind (fun certHash ->
        filePath
        |> List.singleton
        |> partitionBySigned signtool certHash
        |> Result.map (fun (signed, unsigned) ->
            Seq.contains filePath signed && Seq.isEmpty unsigned
        )
    )

  let signIfNotSigned signtool certutil pfx password filePaths =
    pfx
    |> getPfxCertHash certutil password
    |> Result.bind (fun certHash ->
        filePaths
        |> partitionBySigned signtool certHash
        |> Result.bind (fun (toSkip, toSign) ->
            toSkip
            |> Array.length
            |> function
                | 0 -> None
                | 1 -> Some (1, "file", "has")
                | n -> Some (n, "files", "have")
            |> Option.iter (fun (n, files, have) ->
                printfn
                  "Skipping %d %s which %s already been signed with %s"
                  n
                  files
                  have
                  pfx
            )

            toSign
            |> Array.toList
            |> fun files ->
                if List.isEmpty files then
                  Ok None
                else
                  let timestampAlgo = "sha256"
                  let digestAlgo    = "sha256"
                  let timestampUrl  = "http://sha256timestamp.ws.symantec.com/sha256/timestamp"

                  files
                  |> Tools.Signtool.generateSignArgs
                      digestAlgo
                      timestampAlgo
                      timestampUrl
                      pfx
                      password
                  |> Shell.buildStartInfo signtool
                  |> Shell.printCommandFiltered Tools.Signtool.filterPassword
                  |> Shell.runStartInfo
                  |> Result.map Some
        )
    )