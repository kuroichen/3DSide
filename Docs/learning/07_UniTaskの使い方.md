# UniTaskの使い方

コルーチンの代わりに使う非同期処理ライブラリ(Cysharp製)。
「順番に待つ処理」(フェード→移動→フェード解除など)はUniTask、
「繰り返し起きるイベント」はUniRx([06参照](06_UniRxの使い方.md))と使い分けている。

## 基本形: async UniTaskと直列の待ち

コルーチンと違い、途中結果も例外も普通のC#として扱える:

```csharp
public async UniTask FadeOutIn(float duration = -1.0f)
{
    await FadeOut(duration);   // 終わるまで待つ
    await FadeIn(duration);    // その後に実行
}
```

`StageFlowManager.ChangeAreaAsync` が実戦形:「フェードアウト→エリア切替→プレイヤー移動→フェードイン」を上から順に書くだけで表現できている。コルーチンなら `yield return` の入れ子になるところ。

## 撃ちっぱなし: UniTaskVoid + Forget

イベントハンドラから非同期処理を開始したい(が、呼び出し元は待たない)場合:

```csharp
async UniTaskVoid ChangeAreaAsync(AreaTransitionInfo info) { ... }

// 呼ぶ側
ChangeAreaAsync(areaTransition).Forget();
```

- 戻り値を待たない非同期は `UniTask` ではなく **`UniTaskVoid`** にする(待たれない前提が型で分かり、僅かに軽い)
- `.Forget()` を付けないと警告が出る。「意図的に待っていない」の宣言
- **多重実行に注意**: 撃ちっぱなしは連打で並走する。`isChangingArea` / `isRespawning` のようなフラグで入口を塞ぐのがこのプロジェクトの定石

## キャンセル — 破棄されたら止める

`await` 中にオブジェクトが破棄されると、処理が宙に浮いて例外や進行不能の原因になる。必ずCancellationTokenを流す:

```csharp
// フレームを跨ぐ待機には必ずTokenを渡す
await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
```

「新しい操作が来たら前の操作を中断する」には、破棄Tokenとリンクした自前のCTSを作る(`AudioManager` のBGMフェード):

```csharp
this.cts?.Cancel();      // 前の操作を中断
this.cts?.Dispose();
this.cts = CancellationTokenSource.CreateLinkedTokenSource(
    this.GetCancellationTokenOnDestroy());  // 破棄でも中断される
```

キャンセルは `OperationCanceledException` として飛ぶので、握りつぶしてよい場所でcatchする(BGM切り替えは「中断されて当然」なので空catch)。

## UniTaskCompletionSource — イベントとasyncの橋渡し

「別の場所で起きる完了」を待ちたいときに使う。このプロジェクトの実例はリスポーン:

```
死亡通知(Dead) → フェードアウト開始(完了したらTrySetResult)
死亡アニメ終了(Died) → await completionSource.Task  ← フェード完了まで待って合流
                     → リスポーン実行
```

フェードとアニメはどちらが先に終わるか分からない。CompletionSourceを「合流地点」にすることで、両方終わってからリスポーンする、が書ける(`StageFlowManager.OnPlayerDiedAsync`)。

## 細かい作法

- 非同期メソッド名は `〜Async` 接尾辞
- フェードなどポーズ中も動くべき処理は `Time.unscaledDeltaTime` で時間を進める
- `await`可能な待機: `UniTask.Yield`(1フレーム)、`UniTask.Delay`、`UniTask.WaitUntil` など。**`Task.Delay` や `Thread.Sleep` は使わない**(スレッドプールに逃げてUnityのAPIが触れなくなる)
