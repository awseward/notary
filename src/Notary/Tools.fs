namespace Notary

module Tools =
  open Argu
  open System
  open System.Text.RegularExpressions

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

  let private (?>) = defaultArg

  let buildPaths certutil signtool =
    {
      certutil = certutil ?> defaultPaths.certutil
      signtool = signtool ?> defaultPaths.signtool
    }

  let private _printCliArgsFlat<'a when 'a :> IArgParserTemplate> =
    ArgumentParser.Create<'a>().PrintCommandLineArgumentsFlat

  module Signtool =
    type private VerifyArgs =
    | [<Mandatory; First; CliPrefix(CliPrefix.None)>] Verify
    | [<CustomCommandLine("/v")>] Verbose
    | [<Mandatory; CustomCommandLine("/all")>] All
    | [<Mandatory; CustomCommandLine("/pa")>] Pa
    | [<Mandatory; CustomCommandLine("/sha1")>] Sha1 of string
    | [<Mandatory; MainCommand; ExactlyOnce; Last>] Filepaths of string list
    with
      interface IArgParserTemplate with member this.Usage = ""

    type private SignArgs =
    | [<Mandatory; First; CliPrefix(CliPrefix.None)>] Sign
    | [<CustomCommandLine("/v")>] Verbose
    | [<Mandatory; CustomCommandLine("/as")>] AppendSignature
    | [<Mandatory; CustomCommandLine("/fd")>] FileDigestAlgo of string
    | [<Mandatory; CustomCommandLine("/td")>] TimestampDigestAlgo of string
    | [<Mandatory; CustomCommandLine("/tr")>] TimestampServerUrl of string
    | [<Mandatory; CustomCommandLine("/f")>] Pfx of string
    | [<Mandatory; CustomCommandLine("/p")>] Password of string
    | [<MainCommand; ExactlyOnce; Last>] FilesToSign of string list
    with
      interface IArgParserTemplate with member this.Usage = ""


    let generateVerifyArgs sha1 filePaths =
      _printCliArgsFlat <| [
        Verify
        VerifyArgs.Verbose
        All
        Pa
        Sha1 sha1
        Filepaths filePaths
      ]

    let generateSignArgs fileDigestAlgo timestampDigestAlgo timestampServerUrl pfx password filesToSign =
      _printCliArgsFlat <| [
        Sign
        Verbose
        AppendSignature
        FileDigestAlgo fileDigestAlgo
        TimestampDigestAlgo timestampDigestAlgo
        TimestampServerUrl timestampServerUrl
        Pfx pfx
        Password password
        FilesToSign filesToSign
      ]

    let filterPassword commandStr =
      Regex.Replace(commandStr, "/p [^ ]+ ", "/p [FILTERED] ")

  module Certutil =
    type private DumpArgs =
    | [<Mandatory; First; CliPrefix(CliPrefix.Dash)>] Dump
    | [<Mandatory; CustomCommandLine("-p")>] Password of string
    | [<Mandatory; MainCommand; ExactlyOnce; Last>] Pfx of string
    with
      interface IArgParserTemplate with member this.Usage = ""

    let generateDumpArgs password pfx =
      _printCliArgsFlat <| [
        Dump
        Password password
        Pfx pfx
      ]

    let filterPassword commandStr =
      Regex.Replace(commandStr, "-p [^ ]+ ", "-p [FILTERED] ")