module HomePage

open Lit

[<HookComponent>]
let Component () =
  html
    $"""
    <div class="flex flex-col h-screen justify-center items-center">
      <h1 class="mb-4 text-4xl font-extrabold tracking-tight leading-none text-gray-900 md:text-5xl lg:text-6xl dark:text-white">Welcome to Bulletin.</h1>
      <p class="mb-6 text-lg font-normal text-gray-500 lg:text-xl sm:px-16 xl:px-48 dark:text-gray-400">A general use rss reader to fit all of your needs.</p>
    </div>
    """
