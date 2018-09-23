namespace Notary

module Lib =
  open Notary.Types
  open Shell
  open System

  let private _parsePfxCertHash (stdOut: string) =
    let prefix = "Cert Hash(sha1): "

    stdOut
    |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun str -> str.Trim())
    |> Array.filter (fun str -> str.StartsWith prefix)
    |> Array.last // This seems dubious
    |> fun str -> str.Replace(prefix, "")
    |> fun str -> str.Trim()
    |> fun str -> str.Replace(" ", "")
    |> CertHash.create
    |> Result.mapError ErrMsg

  let getPfxCertHash certutil password pfx =
    pfx
    |> Tools.Certutil.generateDumpArgs password
    |> Shell.buildStartInfo certutil
    |> Shell.printCommandFiltered Tools.Certutil.filterPassword
    |> Shell.runStartInfo
    |> Result.bind _parsePfxCertHash

  let partitionBySigned signtool certHash filePaths =
    filePaths
    |> Tools.Signtool.generateVerifyArgs (CertHash.value certHash)
    |> Shell.buildStartInfo signtool
    |> Shell.printCommand
    |> Shell.runStartInfo
    |> Shell.nonzeroExitOk
    |> Result.map (fun output ->
        let prefix = "Successfully verified: "
        let signed =
          output
          |> fun str -> str.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries)
          |> Array.map (fun str -> str.Trim())
          |> Array.filter (fun str -> str.StartsWith prefix)
          |> Array.map (fun str -> str.Replace(prefix, ""))
          |> Set.ofArray

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
                  "Notary: Skipping %d %s which %s already been signed with %s"
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