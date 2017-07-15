open Argu
open Notary
open System
let private _basicFail () =
    printfn "TODO: Usage"
    1
let private _block = Console.ReadLine >> ignore

type DetectArgs =
    | SignTool of path:string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | SignTool _ -> "Specify the path to your signtool.exe"

and NotaryArgs =
    | [<CliPrefix(CliPrefix.None)>] Detect of ParseResults<DetectArgs>
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Detect _ -> "Detect whether a file is already signed by the specified pfx file"

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<NotaryArgs>()

    parser.PrintUsage()
    |> printfn "%s"

    printfn "wat"

    printfn "argv: %A" argv

    // TODO: Put proper CLI parsing in place
    if Array.isEmpty argv then _basicFail()
    else
        // TODO: Unhardcode these
        let certutil = @"C:\WINDOWS\System32\certutil.exe"
        let signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"

        try
            match Array.head argv with
            | "getPfxCertHash" ->
                let pfx = argv.[1]

                pfx
                |> Lib.getPfxCertHash certutil
                |> printfn "%s"

                _block()
                0
            | "checkIfAlreadySigned" ->
                let pfx  = argv.[1]
                let file = argv.[2]

                file
                |> Lib.isFileSignedByPfx signtool certutil pfx
                |> printfn "%b"

                _block()
                0
            | "ensureSigned" ->
                let pfx       = argv.[1]
                let password  = argv.[2]
                let filePaths =
                    argv
                    |> Array.skip 3
                    |> Array.map (fun str -> str.Trim())

                Lib.signIfNotSigned signtool certutil pfx password filePaths

                _block()
                0
            | _ ->
                _basicFail()
        with
        | ex ->
            printfn "%s" ex.Message
            _block()
            _basicFail()
