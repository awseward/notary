namespace Notary.CommandLine

module Args =
    open Argu
    open System

    type DetectArgs =
    | [<Mandatory>] Pfx of ``pfx filepath``:string
    | [<MainCommand; ExactlyOnce; Last>] Files of ``files to check``:string list
    with
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Pfx _ -> "pfx filepath"
                | Files _ -> "files which will be checked for having been signed by the given pfx file"
    and PrintArgs =
    | [<MainCommand; ExactlyOnce; Last>] Pfx of ``pfx filepath``:string
    with
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Pfx _ -> "pfx filepath"
    and SignArgs =
    | [<Mandatory>] Pfx of ``pfx filepath``:string
    | [<Mandatory>] Password of string
    | [<MainCommand; ExactlyOnce; Last>] Files of ``files to check``:string list
    with
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Pfx _ -> "pfx filepath"
                | Password _ -> "password for pfx file"
                | Files _ -> "files which will be checked for having been signed by the given pfx file"
    and NotaryArgs =
    | [<CliPrefix(CliPrefix.None)>] Detect of ParseResults<DetectArgs>
    | [<CliPrefix(CliPrefix.None)>] Print of ParseResults<PrintArgs>
    | [<CliPrefix(CliPrefix.None)>] Sign of ParseResults<SignArgs>
    | [<Inherit>] Certutil of path:string
    | [<Inherit>] Signtool of path:string
    with
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Detect _ -> "detect whether a file is already signed by the specified pfx file"
                | Print _ -> "print the sha1 hash of a given pfx certificate file"
                | Sign _ -> "sign files with the given pfx certificate only if they're not already signed by that same certificate"
                | Certutil _ -> "certutil.exe filepath"
                | Signtool _ -> "signtool.exe filepath"
