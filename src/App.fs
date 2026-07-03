module App

open Elmish
open Elmish.React
open Browser.Dom

// ============================================================
//  エントリポイント
//  Elmish（状態管理）と Feliz/React（描画）を接続します。
// ============================================================

// 開発用：クイズデータの検証結果をブラウザのコンソールに出力します。
// 表示（UI）には影響しません。ブラウザの開発者ツールのコンソールで確認できます。
QuizData.validateQuizData ()
|> List.iter (fun line -> printfn "%s" line)

// ⑤ ページを閉じる/リロードする直前に、進行中クイズを保存します。
window.addEventListener ("beforeunload", (fun _ -> State.saveNow ()))

Program.mkProgram State.init State.update View.view
|> Program.withReactSynchronous "root"
|> Program.run
