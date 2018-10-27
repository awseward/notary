#nowarn "44" // Silence obsolete warnings from FAKE 5

#r "../packages/build/FAKE/tools/FakeLib.dll"

namespace Notary.Fake

module Shims =
  open Fake
  let Target = Target
  let RunTargetOrDefault = RunTargetOrDefault
  let (<==) = (<==)
