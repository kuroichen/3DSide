# 3DSide プロジェクトガイド

Unity製の2.5D横スクロールアクション。キャラクターはSplineベースのレール上を移動する(自由な水平移動は存在しない)。

## 作業前に必ず読むこと

**[Docs/README.md](Docs/README.md) が全ドキュメントの索引。**
クラス構成・参照関係の全体像は [Docs/knowledge/クラス相関図.md](Docs/knowledge/クラス相関図.md) を最初に見る。目的に応じて:

- 設計・実装のルール → `Docs/conventions/`(命名規約、コーディングスタイル)
- 既存システムの仕組み → `Docs/knowledge/`(レール移動、当たり判定、ノックバック、ステージ生成など)
- 企画・ロードマップ → `Docs/planning/`
- 機能仕様・設計書 → `Docs/specs/`

特にレール移動・当たり判定周りを触る前に `Docs/knowledge/` の該当ドキュメントを読むこと。
過去に踏んだ罠と「なぜこの実装なのか」が書いてある。

## 最重要規約(詳細は Docs/conventions/)

- namespace は `Rail`。インスタンスメンバーは `this.` を明示
- インゲーム進行の制御クラスは `〜FlowManager`、常駐サービスは `〜Manager`
- イベント公開は `IObservable<T> OnXxxObservable()` 形式。購読は必ず `.AddTo(this)` か `CompositeDisposable`
- 非同期は UniTask(`〜Async` 接尾辞)。コルーチンは使わない
- ScriptableObject(CharacterStatus等)は読み取り専用。実行中の値はRuntimeクラスへコピーして使う

## 運用ルール

- **コードを変更したら、関連する `Docs/` のドキュメントも同じコミットで更新する**(腐ったドキュメントは無いより悪い)
- 実装中に新しい罠や知見を得たら `Docs/knowledge/` に追記する
- ドキュメントのファイル名は日本語で付ける(README.mdのみ例外)
- ステージはシーンを手編集せず、`Tools/Rail` メニューのCreator(コード)で再生成可能にする
