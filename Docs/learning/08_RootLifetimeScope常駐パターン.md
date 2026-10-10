# RootLifetimeScope常駐パターン

「どのシーンからPlayしても、AudioManagerやフェード用Canvasが必ず存在し、DIで受け取れる」を実現する仕組み。
実装は `RootLifetimeScope.cs` と `Assets/Prefabs/RootLifetimeScope.prefab`、設定は `Assets/Data/VContainerSettings.asset`。

## 解決したい問題

常駐サービス(Audio、画面フェード)をシーンに置くと:

- 全シーンに置く → シーンをまたぐと重複する。置き忘れる
- 専用の起動シーンを最初に読む → **編集中のステージシーンから直接Playできなくなる**(開発が不便)

## 仕組み

```
VContainerSettings.RootLifetimeScope = RootLifetimeScope.prefab
        │ 最初の子LifetimeScopeがBuildされるとき、自動でInstantiate + DontDestroyOnLoad
        ▼
RootLifetimeScope(プレハブ)
 ├ AudioManager       … AudioManager(Mixer Settingsを直接配線)
 └ Canvas             … Screen Space - Overlay
     └ Screen         … 黒の全画面Image + ScreenTransition
```

- 各シーンの `〜LifetimeScope` はRootの子として作られる。Rootに登録したサービスは全シーンで注入できる
- RootはDontDestroyOnLoadされるので、子に置いたAudioManager・ScreenTransitionもシーンをまたいで生き続ける。各コンポーネント側で `DontDestroyOnLoad` やシングルトン化をする必要はない
- プレハブなので、Mixerなどのアセット参照もInspectorで直接配線できる(`Resources.Load` が不要)

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterInstance(this.inputActions);        // アセット
    builder.RegisterComponent(this.audioManager);       // プレハブ内のコンポーネント
    builder.RegisterComponent(this.screenTransition);
    builder.Register<SceneLoader>(Lifetime.Singleton);
}
```

使う側はコンストラクタや `[Inject]` で受け取る(例: `StagePresenter` が `ScreenTransition` を受け取る)。`Instance` 経由のアクセスはしない。

## RegisterInstance と RegisterComponent

どちらも「すでにあるインスタンスを登録する」点は同じ。違いは**登録したオブジェクト自身に注入するか**。

| | 登録したものを他へ渡す | 登録したもの自身への注入(`[Inject]`) | 使う対象 |
|---|---|---|---|
| `RegisterInstance` | ○ | **されない** | アセット(InputActionAsset、ScriptableObject)、プレハブ、ただの値(StageRequest) |
| `RegisterComponent` | ○ | される | シーン/プレハブに置いたMonoBehaviour(View、カメラ、常駐サービス) |

- MonoBehaviourを `RegisterInstance` すると、後から `[Inject]` を足したときに注入されずnullのまま動く。MonoBehaviourは `RegisterComponent` にそろえる
- **プレハブ(アセット)は `RegisterComponent` にしない**。アセット自体に注入しようとしてしまう。生成元として使うだけなので `RegisterInstance`(または `WithParameter` で使う側にだけ渡す)
- 後から生成・ロードするオブジェクトは登録ではなく、`objectResolver.Instantiate`(PlayerFactory、EnemyFactory)や `InjectGameObject`(StageLoader)の時点で注入される

## 「通常時は透明」

RootはTitleでもWorldでも存在するので、ScreenTransitionの `Start Black` はOFF(透明で待機)。
「ステージは暗転から始めたい」はシーン側の都合なので、`StagePresenter` が開始時に `ShowBlackImmediately()` を呼んでから `FadeIn()` する。

## 注意点

- **LifetimeScopeが無いシーンではRootが生成されない**。Rootは子Scopeの生成をきっかけに作られるため、Scopeを置いていないシーン(旧構成のCharacterTestScene、JumpGymなど)からPlayすると、AudioManagerもScreenTransitionも存在しない
- Rootのプレハブで配線が欠けていると(`inputActions` など)、`RegisterInstance(null)` / `RegisterComponent(null)` がNullReferenceExceptionになり、**全シーンのDIが動かなくなる**。スタックトレースに `RootLifetimeScope.Configure` が出たらプレハブの配線を疑う
- プレハブをInspectorで変更したら保存を忘れない(保存されていないと、YAML上は `{fileID: 0}` のまま)

## 経緯: GameBootstrapからの移行

以前は `GameBootstrap`(`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` を付けたstaticクラス)が、最初のシーンより前に常駐オブジェクトをコードで生成し、各コンポーネントはシングルトン(`Instance`)で参照していた。VContainer導入後は「Bootstrapが生成し、RootLifetimeScopeが登録する」と2か所に分かれてしまったため、RootLifetimeScopeに一本化した。

移行時の罠:
- **`RuntimeInitializeOnLoadMethod` はファイルが残っている限りどのシーンでも自動実行される**。呼び出しを部分的に消しただけでBootstrapを残したところ、Bootstrapが生成した不透明な黒Image(sortingOrder 1000)を透明にする処理が無くなり、全画面が真っ黒になった。移行するならファイルごと削除する
- `ScreenTransition.SetAlpha` はImage未設定だと何もせずに戻るため、エラーが出ずに真っ黒になる。見た目だけおかしいときは、Hierarchyの `DontDestroyOnLoad` 配下に想定外のオブジェクトが無いかを見る
