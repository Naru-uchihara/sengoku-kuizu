module App

open Elmish
open Elmish.React

// ============================================================
//  エントリポイント
//  Elmish（状態管理）と Feliz/React（描画）を接続します。
// ============================================================

Program.mkProgram State.init State.update View.view
|> Program.withReactSynchronous "root"
|> Program.run
