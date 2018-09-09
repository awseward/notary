namespace Notary

module Tools =
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

  let private (?>) = defaultArg

  let buildPaths certutil signtool =
    {
      certutil = certutil ?> defaultPaths.certutil
      signtool = signtool ?> defaultPaths.signtool
    }