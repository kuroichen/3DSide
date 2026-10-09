# Bootstrap常駐パターン

「どのシーンからPlayしても、AudioManagerやフェード用Canvasが必ず存在する」を実現する仕組み。
実装は `GameBootstrap.cs`(60行程度なので併読推奨)。

## 解決したい問題

常駐サービス(Audio、画面フェード)をシーンに置くと:

- 全シーンに置く → シーンをまたぐと重複する。置き忘れる
- 専用のBootstrapシーンを最初に読む → **編集中のステージシーンから直接Playできなくなる**(開発が不便)

## 解決策: RuntimeInitializeOnLoadMethod

```csharp
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        var root = new GameObject("[Game Bootstrap]", ...);
        Object.DontDestroyOnLoad(root);
        // ここで常駐コンポーネントをAddComponentしていく
    }
}
```

- この属性を付けたstaticメソッドは、**最初のシーンが読み込まれる前に自動実行される**。どのシーンからPlayしても必ず通る
- 生成したルートを `DontDestroyOnLoad` にして、以降シーンをまたいで生き続ける
- シーンには何も置かないので、置き忘れ・重複が構造的に起きない

## 設定アセットはResources経由で注入する

Bootstrapはコードから生成されるため、Inspectorで参照を配線できない。
アセット参照(AudioMixerなど)は設定用ScriptableObjectにまとめ、`Resources.Load` で取得して注入する:

```csharp
var settings = Resources.Load<AudioMixerSettings>("Audio/AudioMixerSettings");
audioManager.Initialize(settings);
```

- パスは `Assets/Resources/` からの相対。**Resourcesフォルダはビルドに丸ごと含まれる**ので、置くのは設定アセット程度に留める
- 参照が欠けていても起動は止めない(`IsValid` チェック+警告ログ)。「音が鳴らないが遊べる」ように劣化させる

## UIもコードで組める

フェード用の全画面Imageも実行時に生成している。ポイント:

- Canvasの `sortingOrder` を大きく(1000)して最前面を保証
- RectTransformのanchorを `(0,0)〜(1,1)`、offsetを0にすると全画面ストレッチ
- 通常時は透明+`raycastTarget = false`(下のUIへの入力を邪魔しない)。フェード中だけtrueにして入力をブロック(`ScreenTransition`)

## このパターンで動いているもの

| コンポーネント | 役割 |
|---|---|
| `AudioManager` | BGM/SE再生([02参照](02_Audio周りの知見.md)) |
| `ScreenTransition` | 画面フェード([画面フェードの仕組み](../knowledge/画面フェードの仕組み.md)) |

今後、GameFlowManagerやセーブ管理などの常駐サービスもここに追加していく想定([ロードマップ](../planning/ロードマップ.md) Phase 1〜2)。

## 注意点

- Bootstrap生成物とシーン上のオブジェクトの初期化順に依存しない設計にする。シーン側からは `Instance` がnullの可能性を常にケアする(`StageFlowManager` の警告ログ方式)
- 「ステージ開始時は暗転から始めたい」のような**シーン固有の初期状態**はBootstrap側に書かず、シーン側(StageFlowManager)が明示的に指示する。Bootstrapは常に中立(透明)で待機する
