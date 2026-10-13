# ステージYAMLスキーマ設計書

> **注記(2026-10)**: この設計書は旧構成(`StageFlowManager` / `PlayerManager`)を前提に書かれている。両クラスはVContainer + MVP移行で削除し(あわせて `CheckPointInfo` も削除。復帰地点はエリアの入口なので `checkpoint` 要素は不要)、ステージ内進行は `StagePresenter` / `PlayerPresenter`、残機は `PlayerModel` が担う([ステージ進行の仕組み](../knowledge/ステージ進行の仕組み.md))。実装時は読み替えること。

ステージを手続きコード(Creator)ではなく**宣言的なYAML**で定義できるようにする仕組みの設計書。
[レベルデザイン法則集](../planning/レベルデザイン法則集.md)の語彙と設計ルールをスキーマに焼き込む。

## 目的

```
ステージYAML → バリデータ(法則集のルール) → ビルダー(既存ツールを呼ぶ) → シーン生成
```

- ステージ追加を「コードを書く」から「データを書く」に変える(Stage2以降の量産に備える)
- 法則集のアンチパターンを**書けない/書いたら弾かれる**スキーマにする
- LLMがステージ案をYAMLで直接出力できるようにする(法則集+本スキーマがプロンプト素材になる)

## 設計原則

1. **座標はレール距離(m)ベース**。Splineのtは使わない(既存方針と同じ)
2. **寸法はジャンプ性能から導出**。YAMLには「穴2.5m」のような設計意図だけを書き、
   踏切位置・着地マージン等の詳細はインタプリタがジャンプ性能(跳頂3.34m/飛距離4.5m)から計算する
3. **法則をスキーマに焼き込む**:
   - 分岐は上下(Y)ルートのみ(法則B)。`branch` と書けば自動でZレーンに逃げ、
     入口・合流の前後5mは**Z整合ゾーン**として本筋にZを合わせる(法則C・決定事項)
   - 合流は常に飛び降り一方通行(法則B観測)
4. **既存部品の上に乗る**。ジオメトリはStageGeometryUtility、マネージャ配置はStageAutoBuilderの
   外部API(SetMainSpline/SetRoadEnabled/Build)、見た目はStageTheme+StageDecoratorを呼ぶだけ。
   新しい生成ロジックは書かない
5. **冪等**。同じYAMLからは同じシーンが再生成できる(GeneratedStageルート配下を作り直す)
6. **逃げ道を残す**。セクション語彙で表現できない地形は `custom` セクション(knot直書き)で書ける

## ファイル配置と実行

- YAML置き場: `Assets/StageData/<ステージ名>.yaml`(TextAssetとしてインポートされる)
- 実行: `Tools/Rail/Build Stage From YAML` → ファイル選択 → 検証 → 生成
- 検証エラーは「どのセクションが法則集の何番に違反したか」をConsoleに出し、生成を中止する
- パーサはYamlDotNet(Editorアセンブリのみ。ランタイム依存なし)

## スキーマ

### トップレベル

```yaml
stage: Stage2                  # シーン名(Assets/Scenes/<stage>.unity)
theme: FantasyForest           # StageTheme名。無ければ既定テーマにフォールバック
camera: { offset: [0, 3, 14] } # SplineFollowCameraのoffset(省略時は現状維持)
defaults:
  road: { spacing: 2.0, width: 6.0, thickness: 0.8 }   # LayCubesの既定値
areas:                         # エリアの配列。最初の1つが initial
  - id: Area1
    origin: [0, 0, 0]          # このエリアの本筋Splineの開始ワールド座標
    course: [...]              # 平面線形=上から見た経路(省略時は直線)
    sections: [...]            # 縦断線形=本筋の地形(railZ順に前から詰む)
    branches: [...]            # 上ルート(任意)
    events: [...]              # トリガー類
    enemies: [...]             # 敵配置
```

### 平面線形と縦断線形の分離(奥行きの表現)

道路設計と同じく、コースを2つの直交する記述に分ける:

- **course(平面線形)**: 上から見た経路の形。直線とカーブの列
- **sections(縦断線形)**: 高さ方向のプロファイル。レール距離(m)で消費される

カメラは接線から「常に横から見る」を計算するため(カメラ追従の仕組み.md)、
コースがどう曲がってもプレイヤーの見え方・遊びは変わらない。
**縦断がゲームプレイを、平面が景観と世界の説得力を決める**という役割分担になる。

これに伴い、**Zの指定はすべてレールローカル(相対)で解釈する**。
分岐の `lane_z: 7` は「レールの奥側(カメラ反対側)へ7m」であり、ワールドZではない
(散布のScatterSide.FarSideと同じ考え方)。隠し部屋の `z_offset` も同様。

- エリアは `Area◯Contents` に束ねられ、2つ目以降は初期休眠(既存のStageArea方式)

### sections — 本筋の地形語彙

各セクションは前のセクションの終端から続けて敷かれる。`length` の合計がエリアの全長。

| type | パラメータ | 生成されるもの |
|---|---|---|
| `flat` | `length` | 平坦な道 |
| `gaps` | `widths: [2.0, 2.5]`, `interval` | 穴あき道(holeRanges)。穴幅の列と穴間の平地 |
| `hill` | `height`, `rise`, `plateau`, `fall` | 丘。分岐の踏切を兼ねる |
| `steps` | `profile: [[1.5,3],[3.0,3],[1.5,4],[0,4]]` | 高低差の飛び石。[高さ, 手前の穴幅]の列 |
| `slope` | `length`, `rise` | 連続登坂(下りは負)。らせん登りの縦断側 |
| `custom` | `knots: [[x,y,z], ...]`, `holes: [[a,b], ...]` | 逃げ道。knot直書き(座標はエリアローカル) |

- `gaps.widths` の各値はバリデータが飛距離限界(4.5m)と照合する
- `hill` の直後に `branch` の入口が来る場合、踏切として自動配線される

### course — 平面線形

```yaml
course:
  - { straight: 60 }
  - { turn: { angle: 45, radius: 20 } }   # 正=奥曲がり(カメラから遠ざかる)、負=手前曲がり
  - { straight: 80 }
```

- 省略時は全長直線(Stage1と同じ)。sectionsの書き方はコース形状に一切影響されない
- `angle` は累積可能で、**360°を超えるとらせん**になる。登坂は縦断側の `slope` で書く:

```yaml
course:
  - { straight: 40 }
  - turn: { angle: 720, radius: 12, radius_growth: 8 }  # 2周の円錐らせん(1周ごとに半径+8m)
  - { straight: 20 }
sections:
  - { type: flat, length: 40 }
  - { type: slope, length: 176, rise: 10 }  # らせん区間の弧長ぶんで+10m登る
  - { type: flat, length: 20 }
```

らせんの落下ペナルティは**落下死を基本形**とする(決定):

- **円錐らせん(基本形)**: `radius_growth` を道幅+マージン以上(目安8m〜)に設定し、
  周ごとに半径をずらす。上の周の真下に道が無くなるため、落ちると既存の奈落死へ
  そのまま落ちていく。新しいランタイム機構が不要
- **真円らせん(radius_growth: 0)は当面非対応**としてバリデータで弾く。
  真下の周に物理的に着地できてしまうが、プレイヤーの論理位置はレール距離管理のため
  1周分ズレたままになる(着地巻き戻しには「着地時のレール再アタッチ処理」の新設が必要。将来課題)

らせん・急カーブの成立条件(バリデータ対象):

1. **周回ピッチ**: コースが直上に重なる場合、1周あたりの登坂量が頭上クリアランス
   (最低3.4m/推奨4.2〜5.0m)を下回らない(アンチパターン#1の適用)。
   円錐らせんで真下に道が無い場合はこの制約は不要
2. **半径の下限**: 半径 < 道幅程度だと道メッシュが内側で自己交差する。半径≧10mを目安に要求
3. **カメラは外周側**: らせんは**奥曲がり(正のangle)のみ許可**。手前曲がりで周回すると
   カメラがらせん中心側に入り、道自身に視界を遮られる
4. **自己接近**: 折り返し等でコース同士が接近すると最近傍判定の吸い付き・意図しない
   コリジョン接触が起きる。周回を含め、コース間の3D距離が一定値未満になる形状は警告
   (円錐らせんの半径ずらし量もこのチェックで担保する)

- 遠景・地面リボン(幅60m)はらせん区間では自己交差するため、コースがカーブする
  エリアではテーマの `generateGround`/`generateHorizon` の扱いを要検討(未決事項)

### branches — 上ルート

```yaml
branches:
  - id: Middle
    from: 58            # 入口のrailZ(本筋基準)。直前のhillが踏切になる
    to: 122             # 合流点のrailZ。本筋上空で終わり飛び降り(一方通行)
    height: 3.5         # レーン区間の基準Y
    lane_z: 7           # 逃がすZレーン(省略時7)
    z_align: 5          # 入口・合流の前後をZ=本筋に合わせる長さ(省略時5)
    sections: [...]     # レーン上の地形(flatのみでも可)。省略時は全区間flat
    reverse: false      # trueで逆向きSpline(t=1側から乗る)
    branches: [...]     # 入れ子可(3段目)。入れ子のfromは親レーンのrailZ基準
```

インタプリタが自動でやること:

- 入口: 踏切の縁から「前方3m・高低差はジャンプ性能の6〜7割」でknotを計算し、
  入口knotだけ本筋の真上(Z整合)に置いてからlane_zへ逃がす
- 合流: `to` の手前 `z_align` mでZを本筋に戻し、本筋上空(高低差はクリアランス最低値以上)で
  スプラインを終わらせる(端点は本筋に接触させない=吸い付き対策)
- 直上区間(入口・合流のZ整合ゾーン)の頭上クリアランス検証(法則C: 最低3.4m、推奨4.2〜5.0m)

### events — トリガー類

```yaml
events:
  - { type: checkpoint, id: cp-01, at: 78 }        # SplineAnchor+CheckPointInfo
  - { type: door, at: 132, to: Area2 }             # AreaTransitionInfo(遷移扉)
  - { type: goal, at: 98 }                         # GoalInfo(Normal)
  - { type: goal, at: end, on: Top, goal_type: Secret }  # 裏ゴール(分岐レール上)
  - { type: stage_exit, at: 104 }
  - type: hidden_room                              # 隠し部屋一式を丸ごと生成
    at: 70                                         # 頭上トリガーの位置
    trigger_height: 2.6
    room: { length: 24, z_offset: 70 }             # 別エリア+入口/出口/復帰点を自動配線
    hint: true                                     # 法則F: 視覚ヒントの散布(実装は後続)
```

- `at` はそのエリアの本筋railZ(m)。`on: <branch id>` で分岐レール上を指せる。`end` は終端
- `checkpoint` はリスポン用SplineAnchorを同位置に自動生成(エリア休眠の影響を受けないルート直下)
- エリア2以降の `door` 遷移先開始点・入場直後の強制チェックポイント(cp-02方式)は自動生成

### enemies — 敵・アイテム配置

```yaml
enemies:
  - { at: 74 }                  # 本筋のrailZ 74mに既定の敵プレハブ
  - { at: 60, on: Middle }      # 分岐レール上
  - { at: 124, prefab: Assets/Prefabs/Enemy.prefab }  # プレハブ明示(省略時は既定)
```

## バリデーション(法則集アンチパターン表との対応)

生成前に必ず実行。エラー時は生成しない。

| 法則集# | チェック内容 | 実現方法 |
|---|---|---|
| 1 | 頭上クリアランス < 3.4m | 直上区間の本筋Yと分岐床Yの差を計算 |
| 3 | 合流直下±数mに穴・敵 | `branches.to` 近傍の `gaps`・`enemies` を照合 |
| 5 | Z可動域±2mへのコライダー侵入 | lane_z ≥ 4 を要求(±2m+道幅の半分) |
| 7 | 分岐・合流点のZずれ | スキーマ構造上、書けない(自動生成のため常に整合) |
| — | 穴幅 > 飛距離限界4.5m | `gaps.widths`・`steps.profile` を照合 |
| — | 踏切から入口へ届かない | 入口高低差をジャンプ性能と照合 |
| 1 | らせんの周回ピッチ < クリアランス | `course` の周回量と `slope` の登坂量から算出(直上重なり時のみ) |
| 5 | コースの自己接近・自己交差 | course形状の総当たり距離チェック+半径下限+手前曲がり周回の禁止 |
| — | 真円らせん(radius_growth不足) | 落下時のレール位置ズレが未解決のためエラーにする(将来課題) |
| 2, 4 | カメラ関連 | 自動判定困難。当面は対象外(手動確認) |

## Stage1の再表現(スキーマの実力テスト)

実装の受け入れ条件: **Stage1相当のステージがこのスキーマで書けて、生成後に同等に遊べること**。
現時点の語彙でStage1エリア1は次のように書ける見込み:

```yaml
areas:
  - id: Area1
    sections:
      - { type: flat, length: 16 }
      - { type: gaps, widths: [2.0, 2.5, 3.0], interval: 6 }
      - { type: flat, length: 8 }
      - { type: hill, height: 1.5, rise: 3.5, plateau: 7.5, fall: 3 }
      - { type: flat, length: 26 }
      - { type: steps, profile: [[1.5, 3], [3.0, 3], [1.5, 4], [0, 4]] }
      - { type: flat, length: 20 }
    branches:
      - id: Middle
        from: 58
        to: 122
        height: 3.5
        sections:
          - { type: flat, length: 13 }
          - { type: hill, height: 1.5, rise: 3, plateau: 3, fall: 3 }  # 3段目への踏切こぶ
          - { type: flat, length: 40 }
        branches:
          - { id: Top, from: 80, to: 22, height: 8.5, lane_z: 16, reverse: true }
    events:
      - { type: checkpoint, id: cp-01, at: 78 }
      - { type: goal, at: end, on: Top, goal_type: Secret }
      - { type: hidden_room, at: 70, trigger_height: 2.6, room: { length: 24, z_offset: 70 } }
      - { type: door, at: 132, to: Area2 }
    enemies:
      - { at: 74 }
      - { at: 124 }
      - { at: 60, on: Middle }
```

## 実装フェーズ

既存Creatorは残したまま段階的に導入する(Stage1Creatorの置き換えは急がない)。

1. **Phase 1**: パーサ+`flat`/`gaps`/`hill`/`steps`+テーマ適用+`checkpoint`/`goal`/`door`。
   単層ステージがYAMLだけで作れる状態
2. **Phase 2**: `branches`(Z整合ゾーン・入口/合流の自動計算・入れ子・reverse)
3. **Phase 3**: `course`(カーブ・らせん)+`slope`・`hidden_room`・`custom`・バリデータの全ルール
4. **Phase 4**: Stage1をYAMLで再表現して差を検証。以降の新ステージはYAMLを正とする

Phase 1〜2は `course` 省略(直線)のみ対応。スキーマ上は最初から予約されているため、
Phase 3で追加してもYAMLの互換は壊れない。

## 未決事項

- ジャンプ性能(3.34m/4.5m)の取得元: 当面は定数、将来はCharacterStatusから読んで
  性能変更に自動追従させたい
- `steps` の穴を谷に見せる処理(groundHolePadding連携)はテーマ側の既存挙動に任せる
- 敵種別が増えたときの `enemies.prefab` の指定方法(パス直書きか、別名テーブルか)
- `hint: true` の視覚ヒント表現(散布物の配置変更?目立つオブジェクト?)は法則F検証後に設計
- カーブ・らせん区間での地面リボン(幅60m)・遠景の扱い(幅を絞る?生成OFF?テーマ側で対応?)
- SplineFollowCameraの横向き判定(接線×外積)が奥曲がり=外周側になる符号かどうかの実機確認
- 着地時のレール再アタッチ処理(真円らせんの「落下=下の周へ巻き戻し」に必要。
  実現すればスキーマに `fall: land` オプションを追加する)
