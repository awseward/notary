namespace Notary

module Tools =
  open Argu
  open Notary.CommandLine.Args
  open System

  type Paths =
    {
      certutil: string
      signtool: string
    }
  let defaultPaths =
    {
      certutil = @"C:\WINDOWS\System32\certutil.exe"
      signtool = @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x64\signtool.exe"
    }

  let build certutil signtool =
    {
      certutil = certutil |> Option.defaultValue defaultPaths.certutil
      signtool = signtool |> Option.defaultValue defaultPaths.signtool
    }

  let pathsFromParseResults (parseResults: ParseResults<NotaryArgs>) =
    build
      (parseResults.TryGetResult <@ NotaryArgs.Certutil @>)
      (parseResults.TryGetResult <@ NotaryArgs.Signtool @>)
