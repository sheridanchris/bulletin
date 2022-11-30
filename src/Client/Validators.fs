[<RequireQualifiedAccess>]
module Validators

open System
open Validus
open Validus.Operators

let stringExistsValidator (f: char -> bool) (message: ValidationMessage) =
  let rule (value: string) = value |> Seq.exists f
  Validator.create message rule

let stringNotEmptyValidator =
  Check.WithMessage.String.notEmpty (sprintf "%s must not be empty.")

let usernameValidator =
  Check.String.notEmpty
  <+> Check.WithMessage.String.pattern "^[a-zA-Z][a-zA-Z0-9]*$" (sprintf "%s must be alphanumeric.")

let emailAddressValidator =
  Check.WithMessage.String.notEmpty (sprintf "%s must not be empty.")
  <+> Check.WithMessage.String.pattern @"[^@]+@[^\.]+\..+" (sprintf "%s must be an email address.")

let passwordValidator =
  let stringHasSymbolValidator =
    let symbols = "!@#$%^&*()_-+=\\|'\";:,<.>/?"
    stringExistsValidator symbols.Contains

  Check.String.notEmpty
  <+> Check.WithMessage.String.greaterThanLen 6 (sprintf "%s length must be greater 6")
  <+> stringExistsValidator Char.IsLower (sprintf "%s must contain a lowercase character.")
  <+> stringExistsValidator Char.IsUpper (sprintf "%s must contain an uppercase character.")
  <+> stringHasSymbolValidator (sprintf "%s must contain a symbol.")
