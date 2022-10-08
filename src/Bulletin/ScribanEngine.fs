module ScribanEngine

open System.Threading.Tasks
open Falco
open Scriban

type IViewEngine =
    abstract member RenderAsync: view: string * model: 'a -> ValueTask<string>

type ScribanViewEngine(views: Map<string, Template>) =
    interface IViewEngine with
        member _.RenderAsync(view: string, model: 'a) =
            match Map.tryFind view views with
            | Some template -> template.RenderAsync(model)
            | None -> failwithf "View '%s' was not found" view

module Response =
    let renderViewEngine (viewEngine: IViewEngine) (view: string) (model: 'a) : HttpHandler =
        fun ctx ->
            task {
                let! html = viewEngine.RenderAsync(view, model)
                return Response.ofHtmlString html ctx
            }
