# RigidbodyとCharacterController

キャラクター移動の土台をどちらにするかは最初の大きな分岐。
このプロジェクトはCharacterController(以下CC)を採用している。

## 使い分けの基準

| | CharacterController | Rigidbody |
|---|---|---|
| 移動の主導権 | コード(`Move()`で直接動かす) | 物理エンジン(力・速度を与える) |
| 重力 | **自前実装が必要** | 自動 |
| 坂・段差 | slopeLimit / stepOffsetで自動処理 | 自前実装(接地判定+吸着) |
| 外力(ノックバック等) | 自前実装(このプロジェクトは`AddImpulse`) | `AddForce`一発 |
| 他の物理オブジェクトを押す | 押せない(OnControllerColliderHitで自前) | 自然に押し合う |
| 重力方向の変更 | **不可**(上方向がY固定) | 可能 |
| 形状 | カプセル固定・回転不可 | 任意のCollider |

- **キビキビしたアクションの操作感を作るならCC**が楽。物理の「揺り戻し」がなく、毎フレーム位置を完全制御できる。このプロジェクトの非対称重力・可変ジャンプ・コヨーテタイムのような「物理的に嘘のある手触り」はCCだから素直に書けた
- 重力方向を変えたいゲーム(壁歩きなど)はRigidbody一択。このプロジェクトでは将来の宿題として `IRail` 抽象で移行しやすい形だけ作ってある([設計書](../specs/Spline移動リアーキテクチャ設計書.md)参照)

## CCのハマりどころ(実際に踏んだもの)

### isGroundedは信用しすぎない ⚠

`CC.isGrounded` は「直前の `Move()` で下方向に接触したか」でしかない。

- **実際に踏んだ罠⑥**: レール端の角でカプセルの縁が角にかすると接地扱い→落下速度リセット→また離れて落下→また接地…で移動/落下モーションが無限往復した
- 対策: 接地判定を「CCの接触 **かつ** 中心直下のSphereCast(`groundCheckDistance`)」の**AND条件**にした(`CharacterMover`)
- `Move()` を呼ばないフレームは isGrounded が更新されない点にも注意

### テレポートはenabledを切ってから

CCが有効なまま `transform.position` を書き換えると、内部の衝突状態が古いまま残り、
元の位置に押し戻されることがある。ワープさせるときは:

```csharp
characterController.enabled = false;
transform.SetPositionAndRotation(pos, rot);
characterController.enabled = true;
```

(`PlayerView.PlaceAt` がこのパターン)

### その他

- 天井判定を `CollisionFlags.Above` の生値でやると、角かすりでジャンプが殺されることがある(接地側と同じSphereCast化が対策。現状バックログ)
- CCは `OnCollisionEnter` を**発生させない**。CC側が動いてぶつかった通知は `OnControllerColliderHit` で受ける
- 壁判定を自前でやる場合はCapsuleCastをレイヤー絞り込みで(このプロジェクトはTerrain/Gimmickのみ対象)。全レイヤー対象にすると敵や自分の武器に当たる

## Rigidbodyを使う場合のメモ

- 移動系の処理は `FixedUpdate` で。`Update` で `AddForce` すると挙動がフレームレート依存になる
- 自前で動かしたい(が物理判定は欲しい)ときは `isKinematic = true` + `MovePosition()`
- 補間(`interpolation = Interpolate`)を入れないと、FixedUpdate(50fps)とUpdate(可変)の差でカクつく
