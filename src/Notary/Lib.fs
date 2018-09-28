namespace Notary

module Lib =
  open Notary.Types
  open Shell
  open System

  // TODO: Parameterize these or something
  let private _timestampAlgo = "sha256"
  let private _digestAlgo    = "sha256"
  let private _timestampUrl  = "http://sha256timestamp.ws.symantec.com/sha256/timestamp"

  let private _parsePfxCertHash (certutilOutput: string) =
    let prefix = "Cert Hash(sha1): "

    certutilOutput
    |> String.splitStr Environment.NewLine
    |> Array.map String.trim
    |> Array.filter (String.startsWith prefix)
    |> Array.last // This seems dubious
    |> String.replaceStr prefix ""
    |> String.trim
    |> String.replaceStr " " ""
    |> CertHash.create
    |> Result.mapError ErrMsg

  let private _determineSignedAndUnsigned filePaths (certutilOutput: string) =
    let prefix = "Successfully verified: "

    let signed =
      certutilOutput
      |> String.splitStr Environment.NewLine
      |> Array.map String.trim
      |> Array.filter (String.startsWith prefix)
      |> Array.map (String.replaceStr prefix "")
      |> Set.ofArray

    signed
    |> Set.difference (Set.ofList filePaths)
    |> fun unsigned -> (Set.toArray signed, Set.toArray unsigned)

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
    |> Result.map (_determineSignedAndUnsigned filePaths)

  let isFileSignedByPfx signtool certutil password pfx filePath =
    doResult {
      let! certHash = getPfxCertHash certutil password pfx
      let! signed, unsigned = partitionBySigned signtool certHash [filePath]

      return signed |> Seq.contains filePath && Seq.isEmpty unsigned
    }

  let signIfNotSigned signtool certutil pfx password filePaths =
    doResult {
      let! certHash = getPfxCertHash certutil password pfx
      let! signed, unsigned = partitionBySigned signtool certHash filePaths

      signed
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

      if Array.isEmpty unsigned then return None
      else
        let! shellOutput =
          unsigned
          |> List.ofArray
          |> Tools.Signtool.generateSignArgs
              _digestAlgo
              _timestampAlgo
              _timestampUrl
              pfx
              password
          |> Shell.buildStartInfo signtool
          |> Shell.printCommandFiltered Tools.Signtool.filterPassword
          |> Shell.runStartInfo

        return Some shellOutput
    }