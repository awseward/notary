namespace Notary

module Types =
  open System
  open System.Text.RegularExpressions

  module CertHash =
    type _T = CertHash of string

    let create str =
      if Regex.IsMatch (str, "^[A-Fa-f0-9]+$") then
        Ok (CertHash str)
      else
        str
        |> sprintf "Value is not a valid SHA: %s"
        |> Error

    let apply f (CertHash hash) = f hash

    let value = apply id

  type ResultBuilder () =
    member this.Bind (m , f) =
      match m with
      | Ok a -> f a
      | Error a -> Error a
    member this.Return a =
      Ok a
    member this.ReturnFrom m = m

  let doResult = new ResultBuilder ()

  module String =
    let startsWith prefix (input: string) =
      input.StartsWith prefix
    let trim (input: string) =
      input.Trim()
    let replaceStr oldValue (newValue: string) (input: string) =
      input.Replace (oldValue, newValue)
    let splitStr (separator: string) (input: string) =
      input.Split ([|separator|], StringSplitOptions.RemoveEmptyEntries)