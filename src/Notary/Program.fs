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
        let certutilExeFilePath = @"C:\WINDOWS\System32\certutil.exe"
        let signtoolExeFilePath = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"

        try
            match Array.head argv with
            | "getPfxCertHash" ->
                argv
                |> Array.item 1
                |> Lib.getPfxCertHash certutilExeFilePath
                |> printfn "%s"

                _block()
                0
            | "checkIfAlreadySigned" ->
                let pfxFilePath = argv.[1]

                argv
                |> Array.item 2
                |> Lib.isFileSignedByPfx signtoolExeFilePath certutilExeFilePath pfxFilePath
                |> printfn "%b"

                _block()
                0
            | _ ->
                _basicFail()
        with
        | _ -> _basicFail()