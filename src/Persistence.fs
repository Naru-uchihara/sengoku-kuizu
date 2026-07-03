module Persistence

// ============================================================
//  途中保存の永続化レイヤー（localStorage 版）
//  ・UI・状態管理から独立させ、保存/読込/削除/検証だけを担当します。
//  ・将来 DB（Supabase / Firebase 等）へ移す場合は、この関数群の
//    中身（save/load/clear）だけを差し替えれば済みます。
//  ・保存する DTO（SavedProgress）は、後で JSON にしやすいよう
//    プリミティブ型（string / int / bool / 配列）だけで構成しています。
// ============================================================

open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom

/// localStorage のキー
[<Literal>]
let StorageKey = "sengoku_quiz_progress"

// JS の typeof を取るヘルパー（検証に使用）
[<Emit("typeof $0")>]
let private jsTypeof (_: obj) : string = jsNative

/// 現在時刻の ISO 文字列（クイズ開始日時・最終更新日時に使用）
[<Emit("new Date().toISOString()")>]
let nowIso () : string = jsNative

/// 保存用：1問分のデータ（シャッフル後の選択肢順もそのまま保持）
type SavedQuestion =
    { id: int
      text: string
      options: string[]
      answer: string
      explanation: string
      difficulty: string }

/// 保存用：クイズ進行状況
type SavedProgress =
    { category: string          // "busho" / "battle" / "castle"
      difficulty: string         // "beginner" / "intermediate" / "advanced"
      currentIndex: int          // 現在の問題番号（0始まり）
      score: int                 // 現在のスコア
      selectedAnswer: string     // 回答済みの選択肢（未回答は ""）
      isAnswered: bool           // 現在の問題を回答済みか
      questions: SavedQuestion[] // シャッフル後の問題順（＋選択肢順）
      history: bool[]            // 正誤履歴（回答した順）
      answeredOptions: string[]  // 回答済みの選択肢（回答した順）
      startedAt: string          // クイズ開始日時
      updatedAt: string }        // 最終更新日時

/// 保存データを削除する（clearCurrentQuizProgress 相当）
let clearQuizProgress () : unit =
    try
        window.localStorage.removeItem StorageKey
    with _ ->
        ()

/// パースした生オブジェクトが期待する形かを検証する
let validateSavedProgress (o: obj) : bool =
    try
        not (isNull o)
        && jsTypeof o = "object"
        && jsTypeof o?category = "string"
        && jsTypeof o?difficulty = "string"
        && jsTypeof o?currentIndex = "number"
        && jsTypeof o?score = "number"
        && jsTypeof o?isAnswered = "boolean"
        && JS.Constructors.Array.isArray o?questions
        && unbox<int> o?questions?length > 0
    with _ ->
        false

/// 進行状況を保存する
let saveQuizProgress (p: SavedProgress) : unit =
    try
        let json = JS.JSON.stringify p
        window.localStorage.setItem (StorageKey, json)
    with _ ->
        ()

/// 進行状況を読み込む。壊れている/存在しない場合は安全に None を返す
/// （壊れていたデータは削除する）
let loadQuizProgress () : SavedProgress option =
    try
        let raw = window.localStorage.getItem StorageKey
        if isNull raw || raw = "" then
            None
        else
            let parsed = JS.JSON.parse raw
            if validateSavedProgress parsed then
                Some(unbox<SavedProgress> parsed)
            else
                clearQuizProgress ()
                None
    with _ ->
        clearQuizProgress ()
        None

/// 未完了の保存データがあるか
let hasSavedProgress () : bool =
    match loadQuizProgress () with
    | Some _ -> true
    | None -> false
