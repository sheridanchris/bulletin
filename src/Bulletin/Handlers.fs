module Handlers

open Falco
open Falco.Middleware
open ScribanEngine

let scribanViewHandler (view: string) (model: 'a) : HttpHandler =
    withService<IViewEngine> (fun viewEngine -> Response.renderViewEngine viewEngine view model)
