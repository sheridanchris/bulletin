module Interop

open Browser.Types

[<AllowNullLiteral>]
type HTMLDialogElement =
  inherit HTMLElement
  // abstract ``open``: bool with get, set
  // abstract returnValue: string with get, set
  // abstract close: ?returnValue: string -> unit
  // abstract show: unit -> unit
  abstract showModal: unit -> unit
