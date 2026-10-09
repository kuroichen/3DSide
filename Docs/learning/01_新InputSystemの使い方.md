# 新InputSystemの使い方

このプロジェクトで使っている範囲を中心にまとめる。実例は `PlayerController.cs`。

## このプロジェクトの構成

- アクション定義: [InputSystem_Actions.inputactions](../../Assets/InputSystem_Actions.inputactions)
- **Project-wide Actions** として登録済み(Project Settings > Input System Package > Actions)。そのため各シーンに `PlayerInput` コンポーネントを置く必要がない
- コード側は `InputSystem.actions` でどこからでも参照できる:

```csharp
this.moveInput = InputSystem.actions.FindAction("Move");
this.jumpInput = InputSystem.actions.FindAction("Jump");
this.attackInput = InputSystem.actions.FindAction("Attack");
```

## 入力の読み取り3パターン

| API | 意味 | プロジェクトでの用途 |
|---|---|---|
| `ReadValue<Vector2>()` | 現在の値(毎フレームポーリング) | 移動(`GetInput()` でx成分だけ使用) |
| `WasPressedThisFrame()` | このフレームに押されたか | ジャンプ開始・攻撃 |
| `IsPressed()` | 押しっぱなしか | 可変ジャンプ(`IsJumpHeld()` → ジャンプカット判定) |

- 「押した瞬間」と「押しっぱなし」を分けて取れるのが旧Inputとの大きな違い。`CharacterMover` のジャンプバッファ/コヨーテタイム/ジャンプカットはこの区別の上に成り立っている
- コールバック(`action.performed += ...`)方式もあるが、このプロジェクトはFSMの `Update` 内でポーリングする方式で統一している

## ハマりどころ

- **`FindAction` は文字列引数** — アクション名をリネームすると実行時までエラーにならない。名前を変えたら全`FindAction`をgrepすること
- **Action Mapの有効化** — Project-wide Actionsはデフォルトで有効だが、`PlayerInput` 経由や自前のアセット参照の場合は `actionMap.Enable()` を呼ばないと値が取れない(無反応で気づきにくい)
- **`WasPressedThisFrame` はUpdate系でのみ正確** — FixedUpdateで読むと取りこぼす/二重取りすることがある。このプロジェクトはUpdate駆動なので問題なし
- **ポーズとの相性** — `Time.timeScale = 0` でも入力は届く。ポーズメニュー実装時はUI用のAction Mapに切り替える(`Player` Mapを`Disable`、`UI` Mapを`Enable`)のが定石

## 学習リソース

- 公式マニュアル: Input System > Workflows > Project-wide Actions
