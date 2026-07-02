module App

open Elmish
open Elmish.React

// ============================================================
//  エントリポイント
//  Elmish（状態管理）と Feliz/React（描画）を接続します。
// ============================================================

// 開発用：クイズデータの検証結果をブラウザのコンソールに出力します。
// 表示（UI）には影響しません。ブラウザの開発者ツールのコンソールで確認できます。
QuizData.validateQuizData ()
|> List.iter (fun line -> printfn "%s" line)

Program.mkProgram State.init State.update View.view
|> Program.withReactSynchronous "root"
|> Program.run
