open System

open SharpVille.Server.Http

[<EntryPoint>]
let main argv = 
    startServer()

    Console.ReadKey() |> ignore

    0 // return an integer exit code