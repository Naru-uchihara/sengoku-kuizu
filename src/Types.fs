module Types

// ============================================================
//  ドメイン型定義
//  UI・状態・データから独立した「言葉の定義」だけをここに置きます。
//  Supabase / Firebase 等に置き換える際も、この型を基準にすれば
//  データ層とUI層の境界が明確になります。
// ============================================================

/// クイズのジャンル
type Category =
    | Busho   // 武将
    | Battle  // 合戦
    | Castle  // 城

module Category =
    /// データ格納・保存用のキー（"busho" / "battle" / "castle"）
    let toKey =
        function
        | Busho -> "busho"
        | Battle -> "battle"
        | Castle -> "castle"

    /// 画面表示用のジャンル名
    let displayName =
        function
        | Busho -> "武将"
        | Battle -> "合戦"
        | Castle -> "城"

    /// トップ画面のボタンに出す短い説明文
    let description =
        function
        | Busho -> "名将たちの逸話から人物を当てよ"
        | Battle -> "戦の流れ・勝敗・背景を見抜け"
        | Castle -> "名城と城主、所在地を極めよ"

    /// アイコン風の装飾（絵文字）
    let icon =
        function
        | Busho -> "⚔️"
        | Battle -> "🔥"
        | Castle -> "🏯"

    /// テーマ色を表す CSS クラスの接尾辞
    let themeClass =
        function
        | Busho -> "busho"
        | Battle -> "battle"
        | Castle -> "castle"

    /// 全ジャンル
    let all = [ Busho; Battle; Castle ]

/// 難易度
type Difficulty =
    | Beginner      // 初級
    | Intermediate  // 中級
    | Advanced      // 上級

module Difficulty =
    /// データ格納・保存用のキー（"beginner" / "intermediate" / "advanced"）
    let toKey =
        function
        | Beginner -> "beginner"
        | Intermediate -> "intermediate"
        | Advanced -> "advanced"

    /// 画面表示用の難易度名（クイズデータの Difficulty 文字列とも一致）
    let displayName =
        function
        | Beginner -> "初級"
        | Intermediate -> "中級"
        | Advanced -> "上級"

    /// 難易度ボタンに添える短い説明
    let description =
        function
        | Beginner -> "まずは有名どころから"
        | Intermediate -> "戦国好き向け"
        | Advanced -> "腕試しの難問"

    /// テーマ色を表す CSS クラスの接尾辞
    let themeClass =
        function
        | Beginner -> "beginner"
        | Intermediate -> "intermediate"
        | Advanced -> "advanced"

    /// 全難易度
    let all = [ Beginner; Intermediate; Advanced ]

/// 1問分のクイズ
type Question =
    { Id: int
      /// 問題文
      Text: string
      /// 選択肢（4択）
      Options: string list
      /// 正解の選択肢（Options のいずれかと一致する文字列）
      Answer: string
      /// 解説文
      Explanation: string
      /// 難易度（"初級" / "中級" / "上級"）。将来の難易度選択機能で利用予定。
      Difficulty: string }

/// 表示中の画面
type Screen =
    | Top
    | Quiz
    | Result

/// ランク（結果画面の称号）
type Rank =
    { Title: string
      Comment: string }
