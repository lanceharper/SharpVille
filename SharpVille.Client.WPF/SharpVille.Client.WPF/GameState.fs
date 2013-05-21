module GameState

open System.ComponentModel

open SharpVille.Model

type GameState () =
    let mutable state     : State option             = None
    let mutable sessionId : SessionId option         = None
    let mutable gameSpec  : GameSpecification option = None
    let mutable level     : int<lvl>    = 0<lvl>
    let mutable balance   : int64<gold> = 0L<gold>

    let currLevel ()   = match state with | Some { Level = lvl } -> lvl | _ -> 1<lvl>
    let currBalance () = match state with | Some { Balance = balance } -> balance | _ -> 0L<gold>

    let event = Event<_, _>()

    member this.State with get ()        = state
                      and  set value     = state <- value
                                           this.Level <- currLevel()
                                           this.Balance <- currBalance()
    member this.SessionId with get ()    = sessionId
                          and  set value = sessionId <- value
    member this.GameSpec with get ()     = gameSpec
                         and  set value  = gameSpec <- value
    member this.Level with get ()        = level
                      and set value      = level <- value
                                           event.Trigger(this, PropertyChangedEventArgs("Level"))
    member this.Balance with get ()      = balance
                        and set value    = balance <- value
                                           event.Trigger(this, PropertyChangedEventArgs("Balance"))

    member this.ExpPercentage = 
        match this.State, this.GameSpec with
        | Some { Level = lvl; Exp = exp }, Some { Levels = lvls }
            -> match lvls.[lvl], lvls.TryFind (lvl + 1<lvl>) with
               | currLvlExp, Some nextLvlExp -> float (exp - currLvlExp) / float (nextLvlExp - currLvlExp)
               | _ -> 1.0
        | _ -> 0.0

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = event.Publish