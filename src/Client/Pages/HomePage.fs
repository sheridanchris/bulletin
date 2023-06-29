module HomePage

open Lit

[<HookComponent>]
let Component () =
  html
    $"""
    <div class="flex flex-col h-screen justify-center items-center">
      <h1 class="label-text text-9xl font-extrabold">Bulletin</h1>
      <p class="label-text">A general use rss reader to fit all of your needs.</p>
    </div>
    """
