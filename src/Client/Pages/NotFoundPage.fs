module NotFoundPage

open Lit

[<HookComponent>]
let Component () =
  html
    $"""
    <div class="flex h-screen justify-center items-center">
      <h1 class="text-lg">Nothing to see here</h1>
    </div>
    """
