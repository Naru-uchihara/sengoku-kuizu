module State

open Types
open Persistence
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
      /// 選択中の難易度
      Difficulty: Difficulty option
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
      ShowBackConfirm: bool
      /// クイズ開始日時（ISO 文字列）
      StartedAt: string
      /// 回答履歴（新しい順に (選んだ選択肢, 正解かどうか) を積む）
      AnswerHistory: (string * bool) list
      /// トップ画面で「続きから再開」を提示するための、読み込んだ保存データ
      Saved: Persistence.SavedProgress option }

/// 状態を変化させるメッセージ（＝ユーザー操作や画面遷移のトリガー）
type Msg =
    | StartQuiz of Category * Difficulty  // ジャンル・難易度を選んでクイズ開始
    | SelectAnswer of string     // 選択肢を選ぶ
    | NextQuestion               // 次の問題へ（最終問なら結果画面へ）
    | RestartQuiz                // 同じジャンル・難易度でもう一度
    | RequestBackToTop           // クイズ中に戻るボタン → 確認ダイアログを開く
    | ConfirmBackToTop           // ダイアログで「はい」→ トップへ
    | CancelBackToTop            // ダイアログで「いいえ」
    | GoToTop                    // 結果画面などから直接トップへ
    | ResumeQuiz                 // トップの「続きから再開」
    | DiscardProgress            // トップの「最初からやり直す」（保存データ削除）

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

/// 正答率（%）を計算する（0〜100 の整数）
let calculatePercentage (score: int) (total: int) : int =
    if total = 0 then 0
    else int (System.Math.Round(float score / float total * 100.0))

/// 正答率（%）ベースでランク（称号とコメント）を判定する
/// 問題数が何問でも（50問でも20問でも）同じ基準で判定できます。
let calculateRank (score: int) (total: int) : Rank =
    let pct = calculatePercentage score total
    if pct >= 90 then
        { Title = "天下人"
          Comment = "見事。戦国の世を統べる知識量です。" }
    elif pct >= 75 then
        { Title = "大名級"
          Comment = "かなりの実力者。あと一歩で天下人です。" }
    elif pct >= 50 then
        { Title = "侍大将"
          Comment = "基礎知識は十分。さらに深掘りしましょう。" }
    elif pct >= 25 then
        { Title = "足軽"
          Comment = "まだ伸びしろがあります。戦国の沼へようこそ。" }
    else
        { Title = "寺子屋から再出発"
          Comment = "まずは有名武将・有名合戦から学び直しましょう。" }

// ------------------------------------------------------------
//  init / update
// ------------------------------------------------------------

/// トップ画面の状態を作る（保存データの有無を渡す）
let private emptyTop (saved: Persistence.SavedProgress option) : Model =
    { Screen = Top
      Category = None
      Difficulty = None
      Questions = []
      CurrentIndex = 0
      Score = 0
      SelectedAnswer = None
      IsAnswered = false
      ShowBackConfirm = false
      StartedAt = ""
      AnswerHistory = []
      Saved = saved }

// ------------------------------------------------------------
//  途中保存：Model ⇔ 保存 DTO の変換と、保存/削除の副作用
// ------------------------------------------------------------

/// Model から保存用 DTO を作る（ジャンル・難易度が未選択なら None）
let private toSaved (model: Model) : Persistence.SavedProgress option =
    match model.Category, model.Difficulty with
    | Some c, Some d ->
        let questions =
            model.Questions
            |> List.map (fun q ->
                { id = q.Id
                  text = q.Text
                  options = List.toArray q.Options
                  answer = q.Answer
                  explanation = q.Explanation
                  difficulty = q.Difficulty }: SavedQuestion)
            |> List.toArray
        // AnswerHistory は新しい順なので、保存時は古い順（回答した順）に戻す
        let chrono = List.rev model.AnswerHistory
        Some
            ({ category = Category.toKey c
               difficulty = Difficulty.toKey d
               currentIndex = model.CurrentIndex
               score = model.Score
               selectedAnswer = model.SelectedAnswer |> Option.defaultValue ""
               isAnswered = model.IsAnswered
               questions = questions
               history = chrono |> List.map snd |> List.toArray
               answeredOptions = chrono |> List.map fst |> List.toArray
               startedAt = (if model.StartedAt = "" then nowIso () else model.StartedAt)
               updatedAt = nowIso () }: SavedProgress)
    | _ -> None

/// 現在の Model を localStorage に保存する（クイズ進行中のみ）
let saveProgress (model: Model) : unit =
    match toSaved model with
    | Some p -> Persistence.saveQuizProgress p
    | None -> ()

/// 途中保存データを削除する（＝ clearCurrentQuizProgress）
/// 将来スコア履歴の保存を足す場合は、この関数の前後に処理を追加します。
let clearCurrentQuizProgress () : unit = Persistence.clearQuizProgress ()

/// 保存を Cmd 化（副作用を update から分離）
let private saveCmd (model: Model) : Cmd<Msg> =
    Cmd.ofEffect (fun _ -> saveProgress model)

/// 削除を Cmd 化
let private clearCmd: Cmd<Msg> =
    Cmd.ofEffect (fun _ -> clearCurrentQuizProgress ())

/// ページを閉じる/リロードする直前に呼ぶための保存（App.fs から利用）
let mutable private lastModel: Model option = None

let saveNow () : unit =
    match lastModel with
    | Some m when m.Screen = Quiz -> saveProgress m
    | _ -> ()

/// 保存 DTO から Model（クイズ画面）を復元する。壊れていれば None
let private resumeFrom (p: Persistence.SavedProgress) : Model option =
    match Category.ofKey p.category, Difficulty.ofKey p.difficulty with
    | Some c, Some d when not (isNull (box p.questions)) && p.questions.Length > 0 ->
        let questions =
            p.questions
            |> Array.toList
            |> List.map (fun sq ->
                { Id = sq.id
                  Text = sq.text
                  Options = (if isNull (box sq.options) then [] else Array.toList sq.options)
                  Answer = sq.answer
                  Explanation = sq.explanation
                  Difficulty = sq.difficulty })
        let count = List.length questions
        let idx =
            if p.currentIndex < 0 then 0
            elif p.currentIndex >= count then count - 1
            else p.currentIndex
        // 履歴を (選択肢, 正誤) のペアに戻す（新しい順に積み直す）
        let answered = if isNull (box p.answeredOptions) then [||] else p.answeredOptions
        let hist = if isNull (box p.history) then [||] else p.history
        let n = min answered.Length hist.Length
        let answerHistory =
            [ for i in 0 .. n - 1 -> (answered.[i], hist.[i]) ] |> List.rev
        Some
            { Screen = Quiz
              Category = Some c
              Difficulty = Some d
              Questions = questions
              CurrentIndex = idx
              Score = p.score
              SelectedAnswer =
                (if p.isAnswered && p.selectedAnswer <> "" then Some p.selectedAnswer else None)
              IsAnswered = p.isAnswered
              ShowBackConfirm = false
              StartedAt = p.startedAt
              AnswerHistory = answerHistory
              Saved = None }
    | _ -> None

let private topState = emptyTop None

/// 起動時：保存データがあれば読み込み、トップ画面で再開を提示できるようにする
let init () : Model * Cmd<Msg> =
    emptyTop (Persistence.loadQuizProgress ()), Cmd.none

/// 指定ジャンル・難易度のクイズを（問題順・選択肢順ともにシャッフルして）開始する
let private startQuiz (category: Category) (difficulty: Difficulty) (model: Model) : Model =
    let questions =
        QuizData.getQuestions category difficulty
        |> shuffleQuestions
        |> List.map shuffleOptions

    { model with
        Screen = Quiz
        Category = Some category
        Difficulty = Some difficulty
        Questions = questions
        CurrentIndex = 0
        Score = 0
        SelectedAnswer = None
        IsAnswered = false
        ShowBackConfirm = false
        StartedAt = Persistence.nowIso ()
        AnswerHistory = []
        Saved = None }

/// 現在表示中の問題を取り出す（安全に）
let currentQuestion (model: Model) : Question option =
    List.tryItem model.CurrentIndex model.Questions

/// 実際の状態遷移。保存/削除は Cmd で副作用として実行します。
let private updateImpl (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    // ① クイズ開始時に保存
    | StartQuiz(category, difficulty) ->
        let m = startQuiz category difficulty model
        m, saveCmd m

    | SelectAnswer option ->
        // すでに回答済みなら何もしない（他の選択肢を押せないようにする）
        if model.IsAnswered then
            model, Cmd.none
        else
            match currentQuestion model with
            | Some q ->
                let isCorrect = option = q.Answer
                let m =
                    { model with
                        SelectedAnswer = Some option
                        IsAnswered = true
                        Score = (if isCorrect then model.Score + 1 else model.Score)
                        AnswerHistory = (option, isCorrect) :: model.AnswerHistory }
                // ② 回答を選択した時に保存
                m, saveCmd m
            | None -> model, Cmd.none

    | NextQuestion ->
        let nextIndex = model.CurrentIndex + 1
        if nextIndex >= List.length model.Questions then
            // 全問終了 → 結果画面へ。⑥ 完了したので途中保存データを削除
            { model with Screen = Result }, clearCmd
        else
            let m =
                { model with
                    CurrentIndex = nextIndex
                    SelectedAnswer = None
                    IsAnswered = false }
            // ③ 次の問題へ進んだ時に保存
            m, saveCmd m

    | RestartQuiz ->
        // 現在選択中のジャンル・難易度でもう一度
        match model.Category, model.Difficulty with
        | Some category, Some difficulty ->
            let m = startQuiz category difficulty model
            m, saveCmd m
        | _ -> topState, Cmd.none

    | RequestBackToTop -> { model with ShowBackConfirm = true }, Cmd.none

    | CancelBackToTop -> { model with ShowBackConfirm = false }, Cmd.none

    | ConfirmBackToTop ->
        // ④ トップに戻る前に保存し、トップでは「続きから再開」を提示
        emptyTop (toSaved model), saveCmd model

    | GoToTop ->
        // 結果画面から戻る場合は完了済み。念のため保存データを削除
        emptyTop None, clearCmd

    | ResumeQuiz ->
        // 「続きから再開」：保存データから復元。壊れていれば安全にトップへ
        match model.Saved with
        | Some p ->
            match resumeFrom p with
            | Some m -> m, Cmd.none
            | None -> emptyTop None, clearCmd
        | None -> model, Cmd.none

    | DiscardProgress ->
        // 「最初からやり直す」：保存データを削除して通常のトップへ
        emptyTop None, clearCmd

/// update 本体（各遷移の後、最新 Model を保持して beforeunload 保存に備える）
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    let (m, cmd) = updateImpl msg model
    lastModel <- Some m
    m, cmd
