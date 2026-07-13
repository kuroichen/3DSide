# Spline移動 リアーキテクチャ設計書（IRail方式の取り込み）

PlatformerRails (https://github.com/FriendSea/PlatformerRails, MIT) の設計を参考に、
Unity Splines + CharacterController の土台を維持したまま `IRail` 抽象を取り込む。

## 目的

1. **分岐をコライダー（SplineGate）なしで実現**する（`World2Local` の null 返しで判定）
2. 将来 Rigidbody 化（重力方向変更）したくなったときに**移行しやすい形**にしておく

重力方向変更そのものは CharacterController では不可。今回は対象外（将来の宿題）。

## 座標系の定義

レールローカル座標 `Vector3` の意味を固定する：

| 成分 | 意味 |
|---|---|
| `x` | レールに対する**横**オフセット（`Cross(up, tangent)` 方向） |
| `y` | レールに対する**上**オフセット |
| `z` | レール始点からの**距離**（t値ではなく実距離。速度が均一になる） |

> ポイント：今の `SplineMover` は t値で移動してるが、t値は曲率で速度がブレる。
> 距離(`z`)ベースにすると移動速度が一定になる。PlatformerRails / 元記事と同じ思想。

## インターフェース

```csharp
public interface IRail
{
    float Length { get; }                          // レール全長（距離）
    Vector3 Local2World(Vector3 localPosition);    // レール座標 → ワールド
    Vector3? World2Local(Vector3 worldPosition);   // ワールド → レール座標（範囲外/対象外は null）
    Quaternion Rotation(float railZ);              // 距離zでの姿勢
}
```

`World2Local` が `null` を返す ＝「このレール上にいない」。**これが分岐の核心。**

## Unity Splines への対応づけ

| IRail | Unity Splines の実装 |
|---|---|
| `Local2World(pos)` | 距離 `pos.z` → 正規化t → `EvaluatePosition(t)` ＋ `right * pos.x` ＋ `up * pos.y` |
| `World2Local(world)` | `SplineUtility.GetNearestPoint` で最近傍t・距離 → `z`に変換、残差を right/up に分解して `x,y`。範囲外なら null |
| `Rotation(z)` | `EvaluateTangent(t)` から `LookRotation` |
| `Length` | `CalculateLength()` |

距離 `z` ↔ 正規化 `t` の変換は `z / Length` で近似（厳密にやるなら弧長テーブル）。

## クラス構成

```
├── SplineRail : MonoBehaviour, IRail   // 1本のSplineをIRail化（SplineContainerをラップ）
│      OnEnable  → RailManager.Add(this)
│      OnDisable → RailManager.Remove(this)
│
├── RailManager (シングルトン or サービス)
│      登録された全SplineRailを保持
│      World2Local(world): 全railに問い合わせ、null以外で「最も近い」railを返す
│      → これが「近いSplineに自動で沿う」＝分岐
│
└── SplineMover (既存を改修)
       railZ (距離) で位置管理
       移動入力で railZ を増減
       RailManager.World2Local で現在railを判定（乗り換え自動）
```

### SplineRail（新規）
- `SplineContainer` をラップして `IRail` を実装
- `OnEnable/OnDisable` で `RailManager` に自動登録（PlatformerRails の `RailBehaviour` と同じ）

### RailManager（新規）
- 登録された `SplineRail` を全部保持
- `World2Local(world)`：全railに問い合わせ → null以外のうち**横/上オフセットが最小**のrailを採用
- 戻り値に「どのrailか」も持たせる（`(IRail rail, Vector3 local)` のタプル等）

### SplineMover（改修）
- 内部状態を **t値 → railZ（距離）** に変更
- `Update`：
  1. 入力で `railZ += input * speed * dt`
  2. `Local2World` で目標ワールド位置を出す
  3. XZを CharacterController に渡す（今と同じ）
  4. Y は今まで通り重力で別管理（CharacterController維持のため）
- 乗り換え：定期的に `RailManager.World2Local(transform.position)` を呼び、別railが最近傍なら現在railを差し替え

## 既存資産の扱い

| 既存 | どうする |
|---|---|
| `SplineGate` | **廃止可能**（World2Localのnull返し＋RailManagerで分岐するため） |
| `SwitchSpline` | RailManager 経由の乗り換えに置き換え |
| `SplineSampler`（構想） | `SplineRail` に発展・吸収 |
| ステージ制作ツール | そのまま使える（生成するのは同じSplineContainer） |
| CharacterStatus / ステート / アイテム / HUD | **一切変更なし** |

## 移行ステップ（段階的・本流を壊さない）

1. **`IRail` と `SplineRail` を新規作成**（既存に影響なし）。1本のSplineをIRail化できることを確認
2. **`RailManager` を作成**。複数Splineを登録し、`World2Local` で最近傍railが取れることをログで確認
3. **`SplineMover` を railZ ベースに改修**（t値廃止）。1本のレール上で今まで通り動くことを確認
4. **分岐テスト**：2本の並走Splineを置き、`SplineGate` なしで乗り換わることを確認
5. 問題なければ `SplineGate` 系を削除

> 各ステップで動作確認できるので、途中で破綻しない。1〜2は既存コードに触らないので特に安全。

## 将来の拡張（今はやらない）

- **重力方向変更**：`SplineMover` を Rigidbody 化し、`Velocity.y` をレールローカル上方向で扱う。
  `IRail.Rotation` がそのまま重力姿勢になる。CharacterController を捨てる決断が必要になったとき
- **動く足場**：足場用 `SplineRail` の Transform を動かす（PlatformerRails の `LiftRail` 相当）
- **区間メタ情報**：`SplineRail` に速度倍率・重力強度などを持たせる

## 参考

- PlatformerRails: https://github.com/FriendSea/PlatformerRails （MIT License）
- 解説記事: https://friendsea.hateblo.jp/entry/2018/11/17/003903
- 主要クラス: `IRail` / `RailManager` / `CompositeRail` / `MoverOnRails` / `RailBehaviour`
