module View

open Feliz
open Types
open State

// ============================================================
//  画面・コンポーネント（View）
//  状態（Model）を受け取り、UI を描くだけ。副作用は dispatch 経由で
//  Msg を送るのみで、状態自体はここでは変更しません。
//
//  コンポーネント構成:
//    App → TopScreen → CategoryButton
//        → QuizScreen → ProgressBar / AnswerButton
//        → ResultScreen
//  見た目（色・装飾・レイアウト）は styles.css に集約しています。
// ============================================================

let private optionLetters = [| "A"; "B"; "C"; "D" |]

// 背景の和風装飾（家紋・雲・古地図の雰囲気）。中身は装飾のみ。
let private washiBackdrop () =
    Html.div [
        prop.className "washi-backdrop"
        prop.ariaHidden true
        prop.children [
            Html.div [ prop.className "kamon kamon-1"; prop.text "❁" ]
            Html.div [ prop.className "kamon kamon-2"; prop.text "✤" ]
            Html.div [ prop.className "kamon kamon-3"; prop.text "❁" ]
            Html.div [ prop.className "sumi-brush" ]
        ]
    ]

// ------------------------------------------------------------
//  DifficultyButton（難易度ボタン：押すとそのコースが開始）
// ------------------------------------------------------------
let difficultyButton (category: Category) (difficulty: Difficulty) (dispatch: Msg -> unit) : ReactElement =
    Html.button [
        prop.className [ "difficulty-button"; $"diff-{Difficulty.themeClass difficulty}" ]
        prop.onClick (fun _ -> dispatch (StartQuiz(category, difficulty)))
        prop.children [
            Html.span [ prop.className "diff-name"; prop.text (Difficulty.displayName difficulty) ]
            Html.span [ prop.className "diff-desc"; prop.text (Difficulty.description difficulty) ]
        ]
    ]

// ------------------------------------------------------------
//  CategoryCard（丸いジャンルカード ＋ 直下に難易度ボタン）
// ------------------------------------------------------------
let categoryCard (index: int) (category: Category) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "category-block"
        // 順番にふわっと出すアニメーション用の遅延
        prop.style [ style.animationDelay (System.TimeSpan.FromMilliseconds(float (150 * (index + 1)))) ]
        prop.children [
            // 丸いジャンルの意匠（デザインは従来を維持）
            Html.div [
                prop.className [ "category-emblem"; $"cat-{Category.themeClass category}" ]
                prop.children [
                    Html.span [ prop.className "cat-icon"; prop.text (Category.icon category) ]
                    Html.span [ prop.className "cat-title"; prop.text (Category.displayName category) ]
                    Html.span [ prop.className "cat-desc"; prop.text (Category.description category) ]
                ]
            ]
            // 難易度ボタン（初級 / 中級 / 上級）
            Html.div [
                prop.className "difficulty-row"
                prop.children (Difficulty.all |> List.map (fun d -> difficultyButton category d dispatch))
            ]
        ]
    ]

// ------------------------------------------------------------
//  TopScreen（トップ画面）
// ------------------------------------------------------------
let topScreen (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "screen top-screen"
        prop.children [
            Html.header [
                prop.className "top-header"
                prop.children [
                    Html.div [ prop.className "title-seal"; prop.text "戦" ]
                    Html.h1 [ prop.className "app-title"; prop.text "戦国クイズ絵巻" ]
                    Html.p [
                        prop.className "app-subtitle"
                        prop.text "ジャンルと難易度を選んで、戦国知識を試そう"
                    ]
                ]
            ]
            Html.div [
                prop.className "category-list"
                prop.children (Category.all |> List.mapi (fun i c -> categoryCard i c dispatch))
            ]
        ]
    ]

// ------------------------------------------------------------
//  ProgressBar（進捗バー）
// ------------------------------------------------------------
let progressBar (currentIndex: int) (total: int) : ReactElement =
    let answered = currentIndex + 1
    let pct = if total = 0 then 0.0 else float answered / float total * 100.0
    Html.div [
        prop.className "progress-track"
        prop.children [
            Html.div [
                prop.className "progress-fill"
                prop.style [ style.width (length.percent pct) ]
            ]
        ]
    ]

// ------------------------------------------------------------
//  AnswerButton（4択の選択肢ボタン）
// ------------------------------------------------------------
let answerButton
    (letterIndex: int)
    (option: string)
    (question: Question)
    (model: Model)
    (dispatch: Msg -> unit)
    : ReactElement =
    // 回答後の状態に応じてクラスを付与（正解=緑 / 不正解=赤 / それ以外=薄く）
    let stateClass =
        if not model.IsAnswered then
            ""
        elif option = question.Answer then
            "answer-correct"
        elif Some option = model.SelectedAnswer then
            "answer-wrong"
        else
            "answer-dim"

    Html.button [
        prop.className [ "answer-button"; stateClass ]
        prop.disabled model.IsAnswered
        prop.onClick (fun _ -> dispatch (SelectAnswer option))
        prop.children [
            Html.span [ prop.className "answer-label"; prop.text optionLetters.[letterIndex] ]
            Html.span [ prop.className "answer-text"; prop.text option ]
            // 回答後に正誤マークを表示
            if model.IsAnswered && option = question.Answer then
                Html.span [ prop.className "answer-mark"; prop.text "○" ]
            elif model.IsAnswered && Some option = model.SelectedAnswer then
                Html.span [ prop.className "answer-mark"; prop.text "×" ]
        ]
    ]

// ------------------------------------------------------------
//  戻る確認ダイアログ
// ------------------------------------------------------------
let backConfirmDialog (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "dialog-overlay"
        prop.onClick (fun _ -> dispatch CancelBackToTop)
        prop.children [
            Html.div [
                prop.className "dialog-card"
                // カード内クリックで閉じないように伝播を止める
                prop.onClick (fun e -> e.stopPropagation ())
                prop.children [
                    Html.p [
                        prop.className "dialog-text"
                        prop.text "途中で戻ると現在のスコアは失われます。トップに戻りますか？"
                    ]
                    Html.div [
                        prop.className "dialog-actions"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch CancelBackToTop)
                                prop.text "いいえ"
                            ]
                            Html.button [
                                prop.className "btn btn-danger"
                                prop.onClick (fun _ -> dispatch ConfirmBackToTop)
                                prop.text "トップに戻る"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// ------------------------------------------------------------
//  QuizScreen（クイズ画面）
// ------------------------------------------------------------
let quizScreen (model: Model) (dispatch: Msg -> unit) : ReactElement =
    let categoryName =
        model.Category |> Option.map Category.displayName |> Option.defaultValue ""

    let difficultyName =
        model.Difficulty |> Option.map Difficulty.displayName |> Option.defaultValue ""

    let themeClass =
        model.Category |> Option.map Category.themeClass |> Option.defaultValue "busho"

    let total = List.length model.Questions

    match currentQuestion model with
    | None ->
        Html.div [ prop.className "screen"; prop.text "問題が見つかりません。" ]
    | Some q ->
        Html.div [
            prop.className [ "screen quiz-screen"; $"theme-{themeClass}" ]
            prop.children [
                // 上部ヘッダー（ジャンル名・問題番号・スコア）
                Html.div [
                    prop.className "quiz-topbar"
                    prop.children [
                        Html.button [
                            prop.className "back-link"
                            prop.onClick (fun _ -> dispatch RequestBackToTop)
                            prop.text "‹ 戻る"
                        ]
                        Html.div [
                            prop.className "quiz-meta"
                            prop.children [
                                Html.span [
                                    prop.className "quiz-genre"
                                    prop.text $"{categoryName}クイズ｜{difficultyName}"
                                ]
                                Html.span [
                                    prop.className "quiz-count"
                                    prop.text $"{model.CurrentIndex + 1} / {total}"
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "quiz-score"
                            prop.text $"現在の正解数：{model.Score}"
                        ]
                    ]
                ]

                progressBar model.CurrentIndex total

                // 問題カード
                Html.div [
                    prop.className "quiz-card"
                    prop.children [
                        Html.div [
                            prop.className "question-number-badge"
                            prop.text $"第 {model.CurrentIndex + 1} 問"
                        ]
                        Html.p [ prop.className "question-text"; prop.text q.Text ]

                        // 4択
                        Html.div [
                            prop.className "answers-list"
                            prop.children (
                                q.Options
                                |> List.mapi (fun i opt -> answerButton i opt q model dispatch)
                            )
                        ]

                        // 解説＆次へ（回答後のみ表示）
                        if model.IsAnswered then
                            let isCorrect = model.SelectedAnswer = Some q.Answer
                            Html.div [
                                prop.className "explanation-area"
                                prop.children [
                                    Html.div [
                                        prop.className [
                                            "result-tag"
                                            (if isCorrect then "tag-correct" else "tag-wrong")
                                        ]
                                        prop.text (if isCorrect then "正解！" else "不正解…")
                                    ]
                                    Html.p [
                                        prop.className "explanation-text"
                                        prop.text q.Explanation
                                    ]
                                    Html.button [
                                        prop.className "btn btn-primary next-btn"
                                        prop.onClick (fun _ -> dispatch NextQuestion)
                                        prop.text (
                                            if model.CurrentIndex + 1 >= total then "結果を見る ▶"
                                            else "次の問題へ ▶"
                                        )
                                    ]
                                ]
                            ]
                    ]
                ]

                if model.ShowBackConfirm then
                    backConfirmDialog dispatch
            ]
        ]

// ------------------------------------------------------------
//  ResultScreen（結果画面）
// ------------------------------------------------------------
let resultScreen (model: Model) (dispatch: Msg -> unit) : ReactElement =
    let categoryName =
        model.Category |> Option.map Category.displayName |> Option.defaultValue ""

    let difficultyName =
        model.Difficulty |> Option.map Difficulty.displayName |> Option.defaultValue ""

    let total = List.length model.Questions
    let rank = calculateRank model.Score total
    let pct = calculatePercentage model.Score total

    Html.div [
        prop.className "screen result-screen"
        prop.children [
            Html.div [
                prop.className "result-card"
                prop.children [
                    Html.div [ prop.className "result-scroll-top"; prop.text "戦 果 発 表" ]
                    Html.p [ prop.className "result-genre"; prop.text $"{categoryName}クイズ｜{difficultyName}" ]

                    // ランク（強調表示）
                    Html.div [
                        prop.className "rank-box"
                        prop.children [
                            Html.span [ prop.className "rank-label"; prop.text "あなたの称号" ]
                            Html.span [ prop.className "rank-title"; prop.text rank.Title ]
                        ]
                    ]

                    // スコア＆正答率
                    Html.div [
                        prop.className "score-summary"
                        prop.children [
                            Html.div [
                                prop.className "score-item"
                                prop.children [
                                    Html.span [ prop.className "score-num"; prop.text $"{model.Score}" ]
                                    Html.span [ prop.className "score-unit"; prop.text $"/ {total} 問正解" ]
                                ]
                            ]
                            Html.div [
                                prop.className "score-item"
                                prop.children [
                                    Html.span [ prop.className "score-num"; prop.text $"{pct}" ]
                                    Html.span [ prop.className "score-unit"; prop.text "% 正答率" ]
                                ]
                            ]
                        ]
                    ]

                    Html.p [ prop.className "rank-comment"; prop.text rank.Comment ]

                    // アクション
                    Html.div [
                        prop.className "result-actions"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-primary"
                                prop.onClick (fun _ -> dispatch RestartQuiz)
                                prop.text "同じジャンル・難易度でもう一度"
                            ]
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch GoToTop)
                                prop.text "トップに戻る"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// ------------------------------------------------------------
//  App（ルート）: 現在の画面に応じて描き分け
// ------------------------------------------------------------
let view (model: Model) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "app-root"
        prop.children [
            washiBackdrop ()
            Html.main [
                prop.className "app-main"
                prop.children [
                    match model.Screen with
                    | Top -> topScreen dispatch
                    | Quiz -> quizScreen model dispatch
                    | Result -> resultScreen model dispatch
                ]
            ]
            Html.footer [
                prop.className "app-footer"
                prop.text "戦国クイズ絵巻 — モック版"
            ]
        ]
    ]
