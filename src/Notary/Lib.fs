namespace Notary

module Lib =
    open System
    open System.Diagnostics
    open System.Text.RegularExpressions
    open Shell

    // NOTE: This is probably just a temporary crutch
    exception NotaryProcessNonzeroException
    exception NotaryException of Exception

    let private _treatNonzeroAsFatal (proc: Process) stdOut stdErr =
        match proc.ExitCode with
        | 0 -> printfn "%s" stdOut // This whole thing really needs to get some notion of verbose / quiet
        | exitCode ->
            printfn "ERROR (%s exit code %d): %s" proc.StartInfo.FileName proc.ExitCode stdErr
            raise NotaryProcessNonzeroException

    let getPfxCertHash certutil pfx =
        try
            let { proc = proc; stdOut = stdOut; stdErr = stdErr } =
                pfx
                |> sprintf "-dump %s"
                |> Shell.createStartInfo certutil
                |> Shell.printFilteredCommand None
                |> Shell.runSync

            _treatNonzeroAsFatal proc stdOut stdErr

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
            |> Shell.createStartInfo signtool
            |> Shell.printFilteredCommand None
            |> Shell.runSync

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
        let doNotNeedSigning, needSigning =
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
                |> Shell.createStartInfo signtool
                |> Shell.printFilteredCommand (Some (fun str -> Regex.Replace(str, "/p [^ ]+ ", "/p [FILTERED] ")))
                |> Shell.runSync

            _treatNonzeroAsFatal proc stdOut stdErr