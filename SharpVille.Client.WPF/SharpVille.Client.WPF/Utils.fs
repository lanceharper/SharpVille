module Utils

open System.IO
open System.Net.Http
open System.Windows.Media

open SharpVille.Common
open SharpVille.Model
open SharpVille.Model.Requests
open SharpVille.Model.Responses

open GameState

let (|State|_|) (gameState : GameState) = 
    match gameState.State with | Some state -> Some state | _ -> None

let (|Plant|_|) coordinate (gameState : GameState) =
    match gameState.State with 
    | Some { Plants = plants } -> plants.TryFind coordinate
    | _ -> None

let makeWebRequest<'req, 'res> action (req : 'req) = 
    async {
        let url = sprintf "http://localhost:80/SharpVille/%s" action
        use clt = new HttpClient()

        use requestStream = new MemoryStream()
        writeJson req requestStream |> ignore
        requestStream.Position <- 0L

        let! response = clt.PostAsync(url, new StreamContent(requestStream)) |> Async.AwaitTask
        response.EnsureSuccessStatusCode() |> ignore

        let! responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
        return readJson<'res> responseStream
    }

let doHandshake playerId onSuccess = 
    let req = { PlayerId = playerId; Hash = "" }
    let response = makeWebRequest<HandshakeRequest, HandshakeResponse> "Handshake" req
                   |> Async.RunSynchronously
    onSuccess response

let doPlant x y sessionId seedId onSuccess =
    let req = { SessionId = sessionId; Position = (x, y); Seed = seedId }
    let response = makeWebRequest<PlantRequest, PlantResponse> "Plant" req
                   |> Async.RunSynchronously
    onSuccess response

let doHarvest x y sessionId onSuccess =
    let req : HarvestRequest = { SessionId = sessionId; Position = (x, y) }
    let response = makeWebRequest<HarvestRequest, HarvestResponse> "Harvest" req
                   |> Async.RunSynchronously
    onSuccess response