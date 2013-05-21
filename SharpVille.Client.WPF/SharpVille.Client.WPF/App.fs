module MainApp

open System
open System.IO
open System.Net.Http
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Shapes
open FSharpx

open SharpVille.Common
open SharpVille.Model
open SharpVille.Model.Requests
open SharpVille.Model.Responses

open GameState
open Utils

type MainWindow = XAML<"MainWindow.xaml">

let server = "localhost"
let player = "test_player"

let emptyPlotBrush   = Brushes.ForestGreen
let plantedPlotBrush = Brushes.DarkGreen

let window = new MainWindow()
let root   = window.Root

let gameState = GameState()
root.DataContext <- gameState

let setExpBar () = 
    let expWidth = (window.NextLevel :?> Rectangle).Width * gameState.ExpPercentage
    (window.Exp :?> Rectangle).Width <- expWidth

let handshake () = doHandshake player (fun resp ->
    gameState.State <- Some { 
                                PlayerId        = player
                                Exp             = resp.Exp
                                Level           = resp.Level
                                Balance         = resp.Balance
                                FarmDimension   = resp.FarmDimension
                                Plants          = resp.Plants
                            }
    gameState.SessionId <- Some resp.SessionId
    gameState.GameSpec  <- Some resp.GameSpecification
    root.DataContext    <- gameState
    setExpBar())

let plant x y = doPlant x y gameState.SessionId.Value "S1" (fun resp ->
    gameState.State <- Some { gameState.State.Value with
                                Balance = resp.Balance
                                Exp     = resp.Exp
                                Level   = resp.Level
                                Plants  = resp.Plants }
    root.DataContext <- gameState
    setExpBar())

let harvest x y = doHarvest x y gameState.SessionId.Value (fun resp ->
    gameState.State <- Some { gameState.State.Value with
                                Balance = resp.Balance
                                Exp     = resp.Exp
                                Level   = resp.Level
                                Plants  = resp.Plants }
    root.DataContext <- gameState
    setExpBar())

let getPlotText x y =
    match gameState with
    | Plant (x, y) plant
        -> let dueDate = plant.DatePlanted.AddSeconds 30.0
           let now = DateTime.UtcNow
           if now >= dueDate then "Harvest"
           else sprintf "%ds" (dueDate - now).Seconds
    | _ -> "Plant"

let onClick x y (plot : Border) =
    match gameState with
    | Plant (x, y) plant
        -> let dueDate = plant.DatePlanted.AddSeconds 30.0
           let now = DateTime.UtcNow
           if now >= dueDate 
           then harvest x y
                plot.Background <- emptyPlotBrush
    | _ -> plant x y
           plot.Background <- plantedPlotBrush

let setUpFarmPlots (container : Grid) =
    let (Some { FarmDimension = (rows, cols); Plants = plants }) = gameState.State
    let plotWidth  = container.Width / float rows
    let plotHeight = container.Height / float cols 

    { 0..rows-1 } |> Seq.iter (fun _ ->
         new RowDefinition(Height = new GridLength(plotHeight)) 
         |> container.RowDefinitions.Add)
    { 0..cols-1 } |> Seq.iter (fun _ -> 
        new ColumnDefinition() |> container.ColumnDefinitions.Add)

    for rowNum = 0 to rows - 1 do
        for colNum = 0 to cols - 1 do
            let plot = new Border()
            plot.Width  <- plotWidth
            plot.Height <- plotHeight
            plot.Background      <- match gameState with
                                    | Plant (rowNum, colNum) _ -> plantedPlotBrush
                                    | _ -> emptyPlotBrush
            plot.BorderBrush     <- Brushes.Black
            plot.BorderThickness <- new Thickness(0.0)
         
            plot.MouseEnter.Add(fun evt -> plot.BorderThickness <- new Thickness(2.0))
            plot.MouseEnter.Add(fun evt -> 
                let label = new Label()
                label.Content <- getPlotText rowNum colNum
                plot.Child <- label)

            plot.MouseDown.Add(fun evt -> onClick rowNum colNum plot |> ignore)

            plot.MouseLeave.Add(fun evt -> plot.BorderThickness <- new Thickness(0.0))
            plot.MouseLeave.Add(fun evt -> plot.Child <- null)
            
            Grid.SetRow(plot, rowNum)
            Grid.SetColumn(plot, colNum)

            container.Children.Add plot |> ignore

let loadWindow() =
    handshake()        
    setUpFarmPlots window.FarmPlotContainer
    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore