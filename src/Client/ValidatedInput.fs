module ValidatedInput

open Lit
open Validus

type InvalidState<'a> = { Value: 'a; Errors: ValidationErrors }

type ValidationState<'a> =
  | Valid of 'a
  | Invalid of InvalidState<'a>

[<RequireQualifiedAccess>]
module ValidationState =
  let create (validator: 'a -> ValidationResult<'a>) (value: 'a) =
    let validationResult = validator value

    match validationResult with
    | Ok value -> Valid value
    | Error validationErrors ->
      Invalid
        {
          Value = value
          Errors = validationErrors
        }

  let createInvalidWithNoErrors fieldName value =
    Invalid
      {
        Value = value
        Errors = ValidationErrors.create fieldName []
      }

  let value validationState =
    match validationState with
    | Valid value
    | Invalid { Value = value } -> value

  let errors value =
    match value with
    | Valid _ -> Map.empty
    | Invalid invalidState -> invalidState.Errors |> ValidationErrors.toMap

[<HookComponent>]
let ErrorComponent (classes: string) (key: string) (validationState: ValidationState<'a>) =
  let renderError error = html $"<p class={classes}>{error}</p>"

  match validationState |> ValidationState.errors |> Map.tryFind key with
  | Some validationErrors when not (List.isEmpty validationErrors) -> validationErrors |> List.head |> renderError
  | _ -> Lit.nothing
