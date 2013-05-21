module SharpVille.Server.Http

open System
open System.IO
open System.Net

open SharpVille.Server.Handlers
open SharpVille.Server.GameEngine

type HttpListener with
    static member Run (url:string, handler: (HttpListenerRequest -> HttpListenerResponse -> Async<unit>)) =
        let listener = new HttpListener()
        listener.Prefixes.Add url
        listener.Start()

        let getContext = Async.FromBeginEnd(listener.BeginGetContext, listener.EndGetContext)

        async {
            while true do
                let! context = getContext
                Async.Start (handler context.Request context.Response)
        } |> Async.Start

        listener

let startServer () =
    HttpListener.Run("http://*:80/SharpVille/Handshake/", handleReq gameEngine.Handshake) |> ignore
    HttpListener.Run("http://*:80/SharpVille/Plant/", handleReq gameEngine.Plant)         |> ignore
    HttpListener.Run("http://*:80/SharpVille/Harvest/", handleReq gameEngine.Harvest)     |> ignore