[<RequireQualifiedAccess>]
module Validation

open System
open Validus
open Validus.Operators

let notEmpty = Check.WithMessage.String.notEmpty (sprintf "%s must not be empty.")

let stringExistsValidator (f: char -> bool) (message: ValidationMessage) =
  let rule (value: string) = value |> Seq.exists f
  Validator.create message rule

let isAlphanumeric =
  Check.String.notEmpty
  <+> Check.WithMessage.String.pattern "^[a-zA-Z0-9]*$" (sprintf "%s must be alphanumeric.")

let isAlphanumericWithSpaces =
  Check.String.notEmpty
  <+> Check.WithMessage.String.pattern "^[a-zA-Z0-9\w+\s]+$" (sprintf "%s must be alphanumeric.")

let isEmailAddress =
  Check.WithMessage.String.notEmpty (sprintf "%s must not be empty.")
  <+> Check.WithMessage.String.pattern @"[^@]+@[^\.]+\..+" (sprintf "%s must be an email address.")

// TODO: Need to refine this.
let isUrl =
  Check.WithMessage.String.pattern "^(?:https?://)?(?:[\w]+\.)(?:\.?[\w]{2,})(.*?)+$" (sprintf "%s must be a valid url.")

let containsSymbol =
  let symbols = "!@#$%^&*()_-+=\\|'\";:,<.>/?"
  stringExistsValidator symbols.Contains

let isValidPassword =
  Check.String.notEmpty
  <+> Check.WithMessage.String.greaterThanLen 6 (sprintf "%s length must be greater 6")
  <+> stringExistsValidator Char.IsLower (sprintf "%s must contain a lowercase character.")
  <+> stringExistsValidator Char.IsUpper (sprintf "%s must contain an uppercase character.")
  <+> stringExistsValidator Char.IsNumber (sprintf "%s must contain a number.")
  <+> stringExistsValidator Char.IsLetter (sprintf "%s must contain a letter.")
  <+> containsSymbol (sprintf "%s must contain a symbol.")
