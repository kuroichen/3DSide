# GameFlowManager設計書

[ロードマップ](../planning/ロードマップ.md) Phase 1「ゲームループを閉じる」の中核。
2026-07-25の設計議論の合意内容。

## 目的

- タイトル → ステージ → リザルト/ゲームオーバー → タイトル、というゲーム全体の進行を成立させる
- 現状の実質バグ「残機切れでソフトロック」(`OnGameOverObservable` の購読者ゼロ)を解消する

## 責務の3層構造

```
PlayerManager      … 資源の管理。プレイヤー生成・残機。「残機が尽きた」という事実を返すだけ
StageFlowManager   … ステージ内進行。失敗/クリアを検知してGameFlowManagerに報告して終わり
GameFlowManager    … ゲーム全体進行。報告を受けて判断し、シーンを遷移させる
```

- 各層は**下の層の事実を自分の言葉に翻訳して上に渡す**一方通行。上の層は下の層の内部を触らない
- GameFlowManagerの語彙は「シーン」「ステージ」だけ。**PlayerManagerや残機の概念は持たない**
- GameOver中はステージもプレイヤーも存在しない(シーンごと破棄されている)

## GameFlowManagerの仕様

- **Bootstrap常駐組**([Bootstrap常駐パターン](../learning/08_Bootstrap常駐パターン.md))。シーンをまたいで生きる
- ステート:

| ステート | 意味 | 遷移先 |
|---|---|---|
| `Title` | タイトル画面 | → InStage(スタート選択) |
| `InStage` | ステージプレイ中 | → GameOver / Result |
| `GameOver` | ゲームオーバー画面 | → Title(将来: ワールドマップ) |
| `Result` | クリアリザルト画面 | → Title(将来: 次ステージ/ワールドマップ) |

- 入口API(StageFlowManagerから呼ばれる):
  - `NotifyStageFailed()` — 残機切れ。呼ばれた時点で**画面は暗転済み**という契約
  - `NotifyStageCleared(GoalType)` — ゴール到達
- シーン遷移は `SceneManager.LoadScene`(単一シーン方式)+ `ScreenTransition` のフェードで行う

## ゲームオーバーの流れ

```
① PlayerManager.TryRespawn() が false を返す(残機切れ)
② StageFlowManager: フェードアウト完了と合流した上で GameFlowManager.NotifyStageFailed()
③ GameFlowManager: GameOverステートへ → GameOverシーン(または常駐UI)を表示
④ プレイヤーの入力 → タイトルシーンをロード
```

### StageFlowManagerの変更点

- `OnPlayerDiedAsync` 内で `TryRespawn()` の**戻り値で分岐**する(現状は戻り値を無視して失敗時もFadeInしてしまう=ソフトロックの真因)
- 失敗時はFadeInせず(暗転のまま)`NotifyStageFailed()` を呼んで抜ける
- `PlayerManager.OnGameOverObservable` は**フロー制御には使わない**。表示系(UI演出)が必要になったときの購読用として残す

## リトライ=通常開始と同じコードパス

GameFlowManagerがステージを開始する方法は常に「ステージシーンをロードする」だけ。
ロードされたシーンの新しいStageFlowManagerが通常の開始フロー(暗転→スポーン→FadeIn)を走らせる。
**リトライ専用の再初期化コードは作らない**(通常開始と同じ経路なので、リトライ固有のバグが構造的に生まれない)。

## クリアの流れ(同じ枠組み)

```
StageFlowManager.HandleGoal(現状Debug.Logのみ)
  → GameFlowManager.NotifyStageCleared(goalType)
    → Resultステートへ → リザルト表示 → タイトルへ
```

Secret/Fakeゴールの具体挙動はResult実装時に定義する。

## 保留事項(今は作らない)

- **ワールドマップ**: GameOver/Resultの戻り先として将来追加。ステート追加で対応できる形にしておく
- **残機の持ち越し**: 現状残機はPlayerManager(ステージシーン住まい)が持つため、ステージごとにリセットされる。ゲーム全体で共有したくなったら、残機データをゲーム層の常駐資源クラス(PlayerData等。GameFlowManagerには持たせない)へ昇格し、PlayerManagerが開始時に読む形にする
- **セーブ連動**: Phase 5(ステージ進行度)で検討

## 実装順

1. GameFlowManagerの骨格(ステートenum + Notify入口2つ + シーンロード)をBootstrapに追加
2. StageFlowManagerに `TryRespawn` 戻り値の分岐を追加(ソフトロック解消)
3. 最小のタイトルシーン(スタートのみ)+ Build Settings整備
4. GameOver表示(最小: 暗転上にテキスト → 入力でタイトルへ)
5. `HandleGoal` から `NotifyStageCleared` 接続 + 最小リザルト
