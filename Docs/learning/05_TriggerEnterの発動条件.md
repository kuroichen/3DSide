# TriggerEnterの発動条件

「当たってるはずなのにOnTriggerEnterが来ない」は定番のハマりどころ。
発動条件を整理し、このプロジェクトの実例(`CollisionObserver` / 武器判定 / アイテム取得)で確認する。

## 発動に必要な条件(全部AND)

1. **両方にColliderがある**
2. **少なくとも片方が `isTrigger = true`**(両方Triggerでも発動する)
3. **少なくとも片方にRigidbodyがある** — 静的Collider同士は何も起きない
   - 例外: **CharacterControllerは自分が `Move()` で動いたとき、Rigidbodyなしで静的Triggerに対して発動する**。このプロジェクトでプレイヤーがAreaTriggerやアイテムを拾えるのはこのおかげ
4. **Layer Collision Matrixで両レイヤーの交差が有効**(Project Settings > Physics)。コードは正しいのに無反応なとき、まずここを疑う
5. **両方のColliderが有効**(`enabled = true`、GameObjectもactive)

補足:

- Triggerは物理的な押し合いをしない(すり抜ける)。「通知だけ欲しい」用途に使う
- `OnCollisionEnter` の方は「両方 `isTrigger = false`+片方に**非Kinematic**のRigidbody」が条件で、CCはこれを発生させない([04参照](04_RigidbodyとCharacterController.md))

## イベントは「どのGameObjectに」届くか ⚠

メッセージは **Colliderが付いているGameObject** と **そのColliderを保持するRigidbodyのGameObject** に届く。
Rigidbodyなしの子Colliderの場合、**子のGameObjectにしか届かない**(親には伝播しない)。

このプロジェクトでの帰結が2つ:

- `CollisionObserver` は**Colliderと同じGameObjectに付ける必要がある**(Awakeで警告を出している)。プレイヤー本体と武器で別々のObserverを持っているのはこのため
- 受け取った `Collider` は子(手足や武器)かもしれないので、相手のスクリプトを取るときは **`GetComponentInParent`** を使う:

```csharp
// PlayerController: 当たったのは敵の子Colliderかもしれない
col.GetComponentInParent<IDamageable>()?.TakeDamage(...);
```

## 「Enterは1回だけ」の性質と攻撃判定への応用

`OnTriggerEnter` は接触の開始時に1回だけ。重なり続けても再発火しない(それは `OnTriggerStay`)。

ただし**無効だったColliderを重なった状態で有効化すると、その時点でEnterが発火する**。
このプロジェクトの武器判定はこの性質を攻撃ウィンドウとして使っている:

```
待機中: weaponCollisionObserver.SetCollisionEnabled(false)  ← 判定なし
攻撃アニメのOnAttackStartイベント → SetCollisionEnabled(true)
  → この瞬間に重なっている敵にもEnterが発火(振り始めの密着攻撃が当たる)
  → 振り抜き中に新たに触れた敵にもEnterが発火
OnAttackEndイベント → SetCollisionEnabled(false)  ← 次の攻撃でまたEnterから始まる
```

- Collider自体のenabledを切ることで「1回の攻撃で同じ敵に1ヒット」が自然に成立している(多段ヒット防止のHashSet管理が不要)
- 逆に、被弾側で「重なりっぱなしで連続ダメージ」を作りたい場合はEnterでは足りず、Stayか周期的なOverlapが必要になる

## デバッグの手順(無反応なとき)

1. 両オブジェクトのInspectorでCollider/isTrigger/Layerを目視
2. Layer Collision Matrixを確認
3. Rigidbody(またはCC)がどちらかにあるか確認
4. `OnTriggerEnter` に無条件の `Debug.Log` を仕込み、レイヤーフィルタ(`Where`)の手前で確認 — フィルタ条件のミス(レイヤー名のtypoなど)とイベント自体が来ていないのを切り分ける
