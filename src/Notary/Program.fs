// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Notary

let private _basicFail () =
    printfn "TODO: Usage"
    1

[<EntryPoint>]
let main argv =
    // TODO: Put proper CLI parsing in place

    if Array.isEmpty argv then _basicFail()
    else
        match Array.head argv with
        | "getPfxCertHash" ->
            let certUtilExeFilePath = @"C:\WINDOWS\System32\certutil.exe"

            argv
            |> Array.item 1
            |> Lib.extractCertHash certUtilExeFilePath
            |> printfn "%s"

            0
        | _ ->
            _basicFail()
