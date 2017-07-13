// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Notary
open System

let private _basicFail () =
    printfn "TODO: Usage"
    1

let private _block () = Console.ReadLine()

[<EntryPoint>]
let main argv =
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