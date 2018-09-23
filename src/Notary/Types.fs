namespace Notary

module Types =
  open System.Text.RegularExpressions

  module CertHash =
    type _T = CertHash of string

    let create str =
      if Regex.IsMatch (str, "[A-F0-9]+") then
        Ok (CertHash str)
      else
        str
        |> sprintf "Value is not a valid SHA: %s"
        |> Error

    let apply f (CertHash hash) = f hash

    let value = apply id
