# 戦国クイズ絵巻

戦国時代の「武将・合戦・城」から選んで知識を試す、和風クイズゲームのモック版です。
**Fable 5（F# → JavaScript）＋ Elmish ＋ Feliz（React）** で実装した SPA（単一ページアプリ）です。

各ジャンル **20問ずつ・合計60問**、正誤判定・解説・スコア・ランク判定まで実装しています。
バックエンド／DB は不要で、クイズデータはコード内に持っています。

---

## セットアップと起動

前提：**.NET SDK 10**（Fable 5 の実行に必要）と **Node.js 18+**。

```bash
# 依存インストール
dotnet tool restore        # Fable 5 を復元（.config/dotnet-tools.json）
npm install

# 開発サーバー（ホットリロード）
npm start                  # → http://localhost:5173

# 本番ビルド（dist/ に出力）
npm run build

# ビルド成果物のプレビュー
npm run preview
```

> `npm start` / `npm run build` は内部で `dotnet fable`（F#→JS変換）→ `vite`（バンドル）の順に実行します。

---

## ディレクトリ構成と「どのファイルに何を実装したか」

```
sengoku-kuizu/
├─ index.html          … HTMLの土台・フォント読み込み・#root
├─ vite.config.js      … Vite（バンドラ／開発サーバー）設定
├─ package.json        … npmスクリプト（start / build）
└─ src/
   ├─ App.fsproj       … Fableプロジェクト定義（ファイルのコンパイル順・依存パッケージ）
   ├─ Types.fs         … ★ドメイン型（Category / Question / Screen / Rank）
   ├─ QuizData.fs      … ★★クイズデータ本体（60問）とデータ取得関数 getQuestions
   ├─ State.fs         … ★状態管理（Elmishの Model / Msg / update とロジック）
   ├─ View.fs          … ★画面・コンポーネント（TopScreen / QuizScreen / ResultScreen 等）
   ├─ App.fs           … エントリポイント（Elmish と React の接続）
   └─ styles.css       … ★デザイン（和風・赤黒金の配色／レイアウト／アニメーション）
```

**設計の要点：データ・状態・表示を分離**しています。

- **クイズデータ**（`QuizData.fs`）… 問題の中身。UI から独立。
- **状態管理**（`State.fs`）… 「現在の画面・ジャンル・問題番号・スコア・回答・正誤・終了状態」を集中管理。
  仕様どおりの関数（`startQuiz` / `selectAnswer` 相当の `SelectAnswer` / `goToNextQuestion` 相当の `NextQuestion` /
  `showResult`（`NextQuestion` 内で結果画面へ遷移）/ `restartQuiz` / `goToTop` / `calculateRank` /
  `shuffleQuestions` / `shuffleOptions`）を実装。
- **表示**（`View.fs` ＋ `styles.css`）… 状態を受け取って描くだけ。副作用は `dispatch` で `Msg` を送るのみ。

この分離により、`QuizData.fs` の `getQuestions` の中身を差し替えるだけで、
将来 **Supabase / Firebase / 外部API** からデータを取る構成に移行できます（UI・状態側は変更不要）。

---

## クイズデータをどこで編集できるか

すべて **`src/QuizData.fs`** にあります。ジャンルごとに 3 つのリスト（`busho` / `battle` / `castle`）です。

```fsharp
{ Id = 1
  Text = "問題文（武将なら名前を伏せたエピソード等）"
  Options = [ "選択肢A"; "選択肢B"; "選択肢C"; "選択肢D" ]
  Answer = "選択肢B"          // ← 必ず Options の中の文字列と完全一致させる
  Explanation = "解説文" }
```

- **問題文・修正**：該当の `{ ... }` を書き換えるだけ。
- **正解**：`Answer` は `Options` に含まれる文字列と**完全一致**させてください（表示順はシャッフルされますが判定は文字列一致なので壊れません）。

---

## 問題数を増やす方法

1. `src/QuizData.fs` の対象リスト（例：`busho`）に新しい `{ Id = ...; ... }` を**追記**するだけ。
   `Id` はジャンル内で重複しない値にしてください。
2. 出題数は「そのジャンルのリスト全件」を使う設計なので、**追加すれば自動で出題数が増えます**
   （進捗バー・「◯ / 全問数」表示・結果画面の分母もすべて自動追従します）。
3. もし「毎回20問だけ抽選して出す」ようにしたい場合は、`State.fs` の `startQuiz` 内で
   `questions |> List.truncate 20` のように件数を絞ればOKです。

---

## デザインを変更する方法

見た目は **`src/styles.css`** に集約しています。

- **配色・角丸・影・フォント**：ファイル冒頭の `:root { --sengoku-red: ...; --gold: ...; ... }`
  という **CSS変数**を編集すれば、全体の色味・角丸・影をまとめて変更できます。
- **ジャンルごとの色**：`.cat-busho / .cat-battle / .cat-castle`（トップの丸ボタン）と
  `.theme-busho / .theme-battle / .theme-castle`（進捗バー）を編集。
- **アニメーション**：`@keyframes`（`title-rise` / `pop-in` / `rank-reveal` / `shake` 等）を調整。
- **レスポンシブ**：`@media (min-width: 720px)` が PC 表示（丸ボタン横並び等）。
  スマホ縦画面を基準に組み、PC はここで上書きしています。

アイコンは外部画像に依存せず**絵文字**（`Types.fs` の `Category.icon`）と CSS 装飾で表現しています。

---

## 実装済みの機能（仕様対応）

- 3画面遷移：**トップ → クイズ → 結果**（`Screen` 型で管理）
- トップの**丸いジャンルボタン**（色分け・ホバーで浮上・押下で縮む・順番に出現）
- クイズ画面：ジャンル名／問題番号／全問数／スコア／問題文／4択（A〜D）／戻る／次へ
- **正誤表示**：正解＝緑○、不正解＝選択肢が赤×＋正解を緑で提示、回答後は他を押せない
- **解説文**と「次の問題へ／結果を見る」ボタン
- 結果画面：ジャンル名／正解数／**正答率(%)**／**ランク**／コメント／「もう一度」「トップに戻る」
- **問題シャッフル**（`shuffleQuestions`）と**選択肢シャッフル**（`shuffleOptions`。正解は文字列一致で判定するため壊れない）
- 上部**進捗バー**
- クイズ途中の「戻る」で**確認ダイアログ**（「途中で戻ると現在のスコアは失われます。トップに戻りますか？」）

### ランク判定（20問中の正解数）

| 正解数 | 称号 |
|---|---|
| 18〜20 | 天下人 |
| 15〜17 | 大名級 |
| 10〜14 | 侍大将 |
| 5〜9 | 足軽 |
| 0〜4 | 寺子屋から再出発 |

---

## 今後、本番化する場合に追加すべき機能

この構成は下記の拡張を見据えています（データ・状態・表示が分離済みのため差し込みやすい）。

- **データのDB化**：`QuizData.getQuestions` を Supabase / Firebase / 外部API 呼び出しに置換
  （Elmish の `Cmd`＋非同期取得に対応させる）。
- **ユーザーごとのスコア保存・ランキング**：認証＋スコアテーブル。`Model` に履歴を追加。
- **難易度選択（初級／中級／上級）**：`Question` に `Difficulty` フィールドを追加し、`startQuiz` で絞り込み。
- **管理画面からのクイズ追加／AIによる問題自動生成**：データ層をAPI化すれば、投稿・生成した問題をそのまま取得可能。
- **武将解説ページ・合戦の地図・城の画像**：`Question` に関連メタ情報を持たせ、詳細画面を追加。
- **連続正解ボーナス・称号コレクション・SNSシェア・LINE連携・豆知識表示**：
  `Model` に状態（連続正解数・獲得称号）を足し、`View` に表示コンポーネントを追加するだけで対応可能。

---

## 技術スタック

- **Fable 5.5**（F# → JavaScript コンパイラ）
- **Elmish**（The Elm Architecture：Model / Msg / update による状態管理）
- **Feliz**（React を型安全に書くための F# DSL）
- **Vite**（開発サーバー／本番バンドル）
