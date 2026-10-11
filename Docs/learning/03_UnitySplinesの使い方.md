# Unity Splinesの使い方

Splinesパッケージ(com.unity.splines)の知見。このプロジェクトではレール移動の土台
(`SplineRail` / `SplineMover` / `SplineAnchor` / `SplineFollowCamera`)として全面的に使っている。
プロジェクト固有の設計は [レール移動系ナレッジ.md](../knowledge/レール移動系ナレッジ.md) を参照。

## 基本構造

- `SplineContainer`: シーンに置くコンポーネント。中に `Spline`(ノット列)を持つ
- 評価API: `EvaluatePosition(t)` / `EvaluateTangent(t)` / `EvaluateUpVector(t)`。tは0〜1の正規化パラメータ
- 編集はScene Viewのツールで。ノットごとに接線モード(Auto/Bezier/Linear)を選べる

## 最重要: tは「距離」に比例しない ⚠

**Splineのtは弧長(実際の道のり)に比例しない。** ノットの間隔や曲率で歪む。
tを等速で進めると、カーブ区間で移動速度が揺れてガクつく(このプロジェクトで実際に踏んだ罠⑦)。

対策は距離ベースで扱い、評価の直前だけtに変換する:

```csharp
// 距離 → t
var t = SplineUtility.ConvertIndexUnit(
    spline, distance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
```

- 「t × 全長 ≒ 距離」という近似は緩いカーブでもズレる。最初から `ConvertIndexUnit` を使うこと
- このプロジェクトのレールローカル座標zが「t値でなく実距離」なのはこれが理由

## 最近傍点の探索

`SplineUtility.GetNearestPoint` は**3D距離**で最近傍を探す。
横スクロールのように「水平位置だけでレール上の位置を決めたい」場合はこれをそのまま使ってはいけない。

- 実際に踏んだ罠⑤: 坂の上空でジャンプすると「斜め後ろ上」の点が3D最近傍になり、落下中に後ろへ引き戻される無限ループが発生した
- 対策: 水平距離(XZ)最小のrailZを三分探索で求める自前実装に変更(`SplineRail.World2Local`)
- 教訓: **どの軸で「近い」を測るべきかは用途で決まる**。ライブラリのデフォルトを疑う

## その他のハマりどころ

- **Transformスケールは(1,1,1)前提で使うのが安全**。`ConvertIndexUnit` はローカル長、`Length` 系はワールド長で動くものがあり、スケールが掛かると距離系が食い違う
- `EvaluateTangent` の戻りは正規化されていない。向きに使うなら `normalized` を忘れない(`PlayerCharacter.SetSpline` 参照)。またy成分を含むので、水平の向きだけ欲しい場合は `tangent.y = 0` してから正規化する
- tの範囲外(<0, >1)は評価できない。端の外に外挿したい場合は端の位置+接線方向で自前計算する(`SplineRail` の端の余白処理がこれ)

## このプロジェクトでの利用箇所

| クラス | 用途 |
|---|---|
| `SplineRail` | Splineをレール化。World⇔Local変換の数学 |
| `SplineMover` | railZを進めて位置を決める |
| `SplineAnchor` | スポーン地点などをSpline上のtで指す部品 |
| `SplineFollowCamera` | カメラをSplineに沿わせる |
