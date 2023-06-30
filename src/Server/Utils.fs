[<AutoOpen>]
module Utils

open System
open System.Security.Cryptography
open System.Text
open Humanizer
open Validus

module String =
  let equalsIgnoreCase (first: string) (second: string) =
    String.Equals(first, second, StringComparison.OrdinalIgnoreCase)

module DateTime =
  let friendlyDifference (before: DateTime) (after: DateTime) =
    let difference = after - before
    difference.Humanize()

module Gravatar =
  let createUrl (emailAddress: string) =
    use md5 = MD5.Create()

    let emailBytes = Encoding.Default.GetBytes(emailAddress.Trim().ToLowerInvariant())
    let emailHash = md5.ComputeHash(emailBytes)

    let profilePictureHash =
      emailHash
      |> Seq.map (fun byte -> byte.ToString("x2"))
      |> String.concat String.Empty

    $"https://www.gravatar.com/avatar/{profilePictureHash}"

module Validation =
  let failOnValidationErrors (f: unit -> ValidationResult<_>) =
    match f () with
    | Ok _ -> ()
    | Error _ -> failwith "Validation failure."
