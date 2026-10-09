# UniRxの使い方

このプロジェクトのイベント伝達はほぼすべてUniRxで書かれている。
「クラス間の通知は `IObservable` で公開し、購読側が加工する」が背骨。

## 基本形: Subject + Observable公開メソッド

```csharp
// 発行側(非公開のSubjectと、公開用のメソッド)
Subject<int> onLivesChangedSubject = new();
public IObservable<int> OnLivesChangedObservable() => this.onLivesChangedSubject;

// 発火
this.onLivesChangedSubject.OnNext(this.currentLives);

// 購読側
playerManager.OnLivesChangedObservable()
    .Subscribe(SetCurrentLives)
    .AddTo(this);
```

- **Subjectは絶対にpublicにしない**。公開するのは `IObservable` だけ(外から `OnNext` を撃たれるのを防ぐ)
- 命名は `OnXxxObservable()` / `onXxxSubject` で統一([コーディングスタイル](../conventions/コーディングスタイル.md))

## 寿命管理 — AddToを忘れると事故る

購読は解除されるまで生き続ける。破棄済みオブジェクトのメソッドが呼ばれてNullReferenceExceptionになる前に、寿命を紐付ける:

| 方法 | 使いどころ |
|---|---|
| `.AddTo(this)` | 購読者が自分のGameObjectと同じ寿命でよいとき(基本これ) |
| `CompositeDisposable` + `.Clear()` | **購読先が作り直されるとき**。例: `PlayerStatusUI` はプレイヤーがリスポーンで作り直されるたびに、古いプレイヤーへの購読をClearして新しいプレイヤーに購読し直す |
| `Subject.Dispose()` (OnDestroy) | 発行側のSubject自体の後始末 |

リスポーンのような「相手が入れ替わる」購読はこのプロジェクトの頻出パターンなので、`PlayerStatusUI.BindPlayer` を手本にすること。

## パイプラインで加工する

購読前にオペレータで絞り込み・変換する。ifの入れ子より意図が読みやすい:

```csharp
this.collisionObserver.OnTriggerEnterObservable()
    .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Enemy"))  // 絞り込み
    .Select(col => col.GetComponentInParent<ICharacterProperties>())        // 変換
    .Where(properties => properties != null)                                // null除去
    .Subscribe(properties => OnCollected(properties))
    .AddTo(this);
```

- `.Do(x => Debug.Log(...))` はパイプラインの途中に挟めるログ。デバッグ後に1行消すだけで戻せる
- `.AsUnitObservable()` は「値は要らない、起きたことだけ知りたい」への変換(`OnDiedObservable` 参照)

## StartWith — 購読が遅れても初期値を受け取る

イベントは購読前に発火した分を受け取れない。UIのように「今の値も欲しい」購読者は `StartWith` で現在値を頭に足す:

```csharp
playerManager.OnPlayerSpawnedObservable()
    .StartWith(playerManager.CurrentPlayer)  // 既にスポーン済みならそれを最初に流す
    .Where(player => player != null)
    .Subscribe(BindPlayer)
    .AddTo(this);
```

`Start` の実行順は保証されないため、「相手が先に発火してしまった」タイミング問題をこのパターンで吸収している。

## このプロジェクトでの使い分けの整理

| 通知の種類 | 実装 |
|---|---|
| 継続的に起きるイベント(被弾、スポーン、エリアイベント) | Subject + Observable |
| Unityの物理/アニメイベントの変換 | Observer部品([当たり判定](../knowledge/当たり判定の仕組み.md) / [アニメーション連携](../knowledge/アニメーション連携の仕組み.md)) |
| 1回きりの完了待ち(フェード完了など) | UniRxではなく**UniTask**([07参照](07_UniTaskの使い方.md)) |
