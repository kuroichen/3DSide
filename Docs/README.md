# Docs

3DSideプロジェクトのドキュメント置き場。Unityにインポートさせないため `Assets/` の外に置く。

## フォルダ構成

| フォルダ | 内容 | 例 |
|---|---|---|
| [planning/](planning/) | 企画・ロードマップ・TODO | ロードマップ、コンセプト |
| [specs/](specs/) | 設計書・機能仕様 | レール移動の設計書、GameFlowManager設計 |
| [conventions/](conventions/) | コード規約・設計規則 | 命名規約、コーディングスタイル |
| [knowledge/](knowledge/) | 実装で得た知見・踏んだ罠 | レール系の挙動メモ |
| [learning/](learning/) | Unity/C#の機能・使い方の学習内容 | 新InputSystem、Audio周り |

## 置き場所の判断基準

- **これから作るものの決め事** → specs/
- **作る前の構想・順序づけ** → planning/
- **書き方・名付け方のルール** → conventions/
- **作った後に分かったこと・ハマりどころ**(このプロジェクト固有) → knowledge/
- **Unity/C#そのものの機能・使い方**(プロジェクトを離れても使える) → learning/

**ファイル名は日本語**で付ける(READMEのみ例外)。learning/ は「インデックス番号_日本語タイトル.md」(例: `01_新InputSystemの使い方.md`)で、番号は追加順の連番。

## 現在のドキュメント

- [planning/ロードマップ.md](planning/ロードマップ.md) — 基本的なゲームとして完成させるためのロードマップ
- [planning/レベルデザイン法則集.md](planning/レベルデザイン法則集.md) — でろでろ道中分析由来のステージ設計指針とアンチパターン(数値は仮説)
- [specs/Spline移動リアーキテクチャ設計書.md](specs/Spline移動リアーキテクチャ設計書.md) — IRail方式の設計書
- [specs/GameFlowManager設計書.md](specs/GameFlowManager設計書.md) — ゲーム全体進行の3層構造とゲームオーバー/クリアの流れ
- [specs/ステージYAMLスキーマ設計書.md](specs/ステージYAMLスキーマ設計書.md) — ステージを宣言的YAMLで定義する仕組み(法則集をスキーマに焼き込む)
- [specs/Stage1新設計.md](specs/Stage1新設計.md) — 法則集準拠のStage1再設計(設計YAMLと検算。Stage1Creatorの正)
- [conventions/命名規約.md](conventions/命名規約.md) — FlowManager / Manager の使い分けなど
- [conventions/コーディングスタイル.md](conventions/コーディングスタイル.md) — 既存コードからの抽出ドラフト
- [knowledge/クラス相関図.md](knowledge/クラス相関図.md) — 全体の参照関係・継承・責務早見表(まずこれ)
- [knowledge/レール移動系ナレッジ.md](knowledge/レール移動系ナレッジ.md) — レール移動の詳細・踏んだ罠
- [knowledge/Spline移動の仕組み.md](knowledge/Spline移動の仕組み.md) — 入門・毎フレームの流れ
- [knowledge/当たり判定の仕組み.md](knowledge/当たり判定の仕組み.md) — レイヤー/CollisionObserver/攻撃ウィンドウ
- [knowledge/ノックバックの仕組み.md](knowledge/ノックバックの仕組み.md) — ワールド→レール空間の翻訳
- [knowledge/ステージ作成パイプライン.md](knowledge/ステージ作成パイプライン.md) — Tools/Railメニューのツール群とテーマシステム
- [knowledge/ステージ進行の仕組み.md](knowledge/ステージ進行の仕組み.md) — AreaTrigger/エリア切替/リスポーンの流れ
- [knowledge/アニメーション連携の仕組み.md](knowledge/アニメーション連携の仕組み.md) — Animation Event/Observer、イベント名とパラメータ一覧
- [knowledge/キャラクターステータスの仕組み.md](knowledge/キャラクターステータスの仕組み.md) — SO定義→Runtime実体、インターフェース分離
- [knowledge/カメラ追従の仕組み.md](knowledge/カメラ追従の仕組み.md) — 接線と外積で「常に横から見る」
- [knowledge/画面フェードの仕組み.md](knowledge/画面フェードの仕組み.md) — ScreenTransitionのAPIと設計方針
- [learning/01_新InputSystemの使い方.md](learning/01_新InputSystemの使い方.md) — Project-wide Actions、入力読み取り3パターン、ハマりどころ
- [learning/02_Audio周りの知見.md](learning/02_Audio周りの知見.md) — AudioResource/Mixer/dB変換/AudioSourceプール
- [learning/03_UnitySplinesの使い方.md](learning/03_UnitySplinesの使い方.md) — tと弧長の罠、最近傍点探索、端の外挿
- [learning/04_RigidbodyとCharacterController.md](learning/04_RigidbodyとCharacterController.md) — 使い分け基準、isGroundedの罠、テレポート作法
- [learning/05_TriggerEnterの発動条件.md](learning/05_TriggerEnterの発動条件.md) — 発動条件AND一覧、イベントの届き先、攻撃ウィンドウへの応用
- [learning/06_UniRxの使い方.md](learning/06_UniRxの使い方.md) — Observable公開パターン、寿命管理、StartWith
- [learning/07_UniTaskの使い方.md](learning/07_UniTaskの使い方.md) — UniTaskVoid/キャンセル/CompletionSource
- [learning/08_Bootstrap常駐パターン.md](learning/08_Bootstrap常駐パターン.md) — RuntimeInitializeOnLoadMethodによる常駐サービス起動
