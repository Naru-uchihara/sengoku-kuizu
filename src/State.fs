module State

open Types
open Elmish

// ============================================================
//  状態管理（The Elm Architecture: Model / Msg / init / update）
//  画面表示（View）から完全に分離しています。
//  ここには「アプリの状態」と「状態を変化させるロジック」だけを置きます。
// ============================================================

/// アプリ全体の状態
type Model =
    { /// 現在の画面（Top / Quiz / Result）
      Screen: Screen
      /// 選択中のジャンル
      Category: Category option
      /// 今回の挑戦で出題する問題（シャッフル済み・選択肢もシャッフル済み）
      Questions: Question list
      /// 現在の問題番号（0 始まり）
      CurrentIndex: int
      /// 現在のスコア（正解数）
      Score: int
      /// 選択済みの回答（未回答なら None）
      SelectedAnswer: string option
      /// 回答済みかどうか（正誤判定を表示中か）
      IsAnswered: bool
      /// 「途中でトップに戻る」確認ダイアログの表示状態
      ShowBackConfirm: bool }

/// 状態を変化させるメッセージ（＝ユーザー操作や画面遷移のトリガー）
type Msg =
    | StartQuiz of Category      // ジャンルを選んでクイズ開始
    | SelectAnswer of string     // 選択肢を選ぶ
    | NextQuestion               // 次の問題へ（最終問なら結果画面へ）
    | RestartQuiz                // 同じジャンルでもう一度
    | RequestBackToTop           // クイズ中に戻るボタン → 確認ダイアログを開く
    | ConfirmBackToTop           // ダイアログで「はい」→ トップへ
    | CancelBackToTop            // ダイアログで「いいえ」
    | GoToTop                    // 結果画面などから直接トップへ

// ------------------------------------------------------------
//  ユーティリティ：シャッフル（Fisher–Yates）
// ------------------------------------------------------------
let private rng = System.Random()

/// リストをランダムに並べ替える（純粋関数として新しいリストを返す）
let private shuffle (items: 'a list) : 'a list =
    let arr = List.toArray items
    let n = arr.Length
    for i in n - 1 .. -1 .. 1 do
        let j = rng.Next(i + 1)
        let tmp = arr.[i]
        arr.[i] <- arr.[j]
        arr.[j] <- tmp
    Array.toList arr

/// 問題の出題順をシャッフルする
let shuffleQuestions (questions: Question list) : Question list = shuffle questions

/// 1問の選択肢の表示順をシャッフルする（正解の値は変えないので判定は壊れない）
let shuffleOptions (question: Question) : Question =
    { question with Options = shuffle question.Options }

/// 正解数からランク（称号とコメント）を判定する
let calculateRank (score: int) : Rank =
    if score >= 18 then
        { Title = "天下人"
          Comment = "見事。戦国の世を統べる知識量です。" }
    elif score >= 15 then
        { Title = "大名級"
          Comment = "かなりの実力者。あと一歩で天下人です。" }
    elif score >= 10 then
        { Title = "侍大将"
          Comment = "基礎知識は十分。さらに深掘りしましょう。" }
    elif score >= 5 then
        { Title = "足軽"
          Comment = "まだ伸びしろがあります。戦国の沼へようこそ。" }
    else
        { Title = "寺子屋から再出発"
          Comment = "まずは有名武将・有名合戦から学び直しましょう。" }

// ------------------------------------------------------------
//  init / update
// ------------------------------------------------------------

/// トップ画面の初期状態
let private topState =
    { Screen = Top
      Category = None
      Questions = []
      CurrentIndex = 0
      Score = 0
      SelectedAnswer = None
      IsAnswered = false
      ShowBackConfirm = false }

let init () : Model * Cmd<Msg> = topState, Cmd.none

/// 指定ジャンルのクイズを（問題順・選択肢順ともにシャッフルして）開始する
let private startQuiz (category: Category) (model: Model) : Model =
    let questions =
        QuizData.getQuestions category
        |> shuffleQuestions
        |> List.map shuffleOptions

    { model with
        Screen = Quiz
        Category = Some category
        Questions = questions
        CurrentIndex = 0
        Score = 0
        SelectedAnswer = None
        IsAnswered = false
        ShowBackConfirm = false }

/// 現在表示中の問題を取り出す（安全に）
let currentQuestion (model: Model) : Question option =
    List.tryItem model.CurrentIndex model.Questions

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | StartQuiz category -> startQuiz category model, Cmd.none

    | SelectAnswer option ->
        // すでに回答済みなら何もしない（他の選択肢を押せないようにする）
        if model.IsAnswered then
            model, Cmd.none
        else
            match currentQuestion model with
            | Some q ->
                let isCorrect = option = q.Answer
                { model with
                    SelectedAnswer = Some option
                    IsAnswered = true
                    Score = if isCorrect then model.Score + 1 else model.Score },
                Cmd.none
            | None -> model, Cmd.none

    | NextQuestion ->
        let nextIndex = model.CurrentIndex + 1
        if nextIndex >= List.length model.Questions then
            // 全問終了 → 結果画面へ
            { model with Screen = Result }, Cmd.none
        else
            { model with
                CurrentIndex = nextIndex
                SelectedAnswer = None
                IsAnswered = false },
            Cmd.none

    | RestartQuiz ->
        match model.Category with
        | Some category -> startQuiz category model, Cmd.none
        | None -> topState, Cmd.none

    | RequestBackToTop -> { model with ShowBackConfirm = true }, Cmd.none

    | CancelBackToTop -> { model with ShowBackConfirm = false }, Cmd.none

    | ConfirmBackToTop -> topState, Cmd.none

    | GoToTop -> topState, Cmd.none
