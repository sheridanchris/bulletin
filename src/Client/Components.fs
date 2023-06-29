module Components

open System
open Lit

module Text =
  [<HookComponent>]
  let ErrorComponent (error: string) =
    html $"""<p class="text-sm text-red-500">{error}</p>"""

module Alerts =
  type AlertModel = { Reason: string }

  type Alert =
    | Success of AlertModel
    | Info of AlertModel
    | Warning of AlertModel
    | Danger of AlertModel

  let renderAlert (alert: Alert) =
    let reason, alertType =
      match alert with
      | Success alert -> alert.Reason, "alert-success"
      | Info alert -> alert.Reason, "alert-info"
      | Warning alert -> alert.Reason, "alert-warning"
      | Danger alert -> alert.Reason, "alert-error" 

    html
      $"""
        <div class="alert {alertType} w-fit pb-1">
          <i class="fa-solid fa-circle-info"></i>
          <div>
            <span class="font-medium">{reason}</span>
          </div>
        </div>
        """

module ValidationErrors =
  let renderValidationErrors (validationErrors: Map<string, string list>) (key: string) (inputValue: string) =
    if String.IsNullOrWhiteSpace inputValue then
      Lit.nothing // DON'T RENDER THE VALIDATION ERROR IF THE INPUT IS EMPTY.
    else
      let error =
        validationErrors |> Map.tryFind key |> Option.map List.tryHead |> Option.flatten

      match error with
      | None -> Lit.nothing
      | Some error -> Text.ErrorComponent error
