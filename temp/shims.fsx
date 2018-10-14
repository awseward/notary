#r "../packages/build/FAKE/tools/FakeLib.dll"

namespace Notary.Fake

module Shims =
  open Fake
  let Target = Target
  let RunTargetOrDefault = RunTargetOrDefault
  let (<==) = (<==)
