using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// 1ステージ目(要素見本市)を自動生成するメニュー。すり抜け床は不使用(全て通常コリジョン)。
    /// ① 穴あき道: 2 / 2.5 / 3m の穴を跳んで渡る(落ちたら奈落死→リスポン)
    /// ② 三層分岐(互い違い配置):
    ///    - 丘(y1.5)から前方ジャンプで2段目(y3.5)へ。2段目は+z側へ膨らみ、飛び石を
    ///      スキップしてかなり先で1段目に端点合流(2段目を選んだご褒美)
    ///    - 2段目の入口で振り返って後ろ上方へ跳ぶと3段目(y5.5)。3段目は逆向きSplineで
    ///      t=1.0側から乗る検証を兼ねる。逆走方向へ+z側に曲がり、裏ゴールで行き止まり
    /// ③ 高低差の飛び石(1段目のみ): 低→中→高→中→低。登りは3m間隔、下りは4m間隔
    /// 寸法はジャンプ性能(跳頂3.34m/飛距離限界4.5m)から逆算。
    /// 敵: 地上2体 + 2段目1体 (CharacterTestSceneのEnemyをプレハブ化して使用)
    /// Library/create_stage1_request が存在する場合はコンパイル直後に一度だけ自動実行する(外部ツール連携用)。
    /// </summary>
    public static class Stage1Creator
    {
        const string RequestMarkerPath = "Library/create_stage1_request";
        const string ScenePath = "Assets/Scenes/Stage1.unity";
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        const string CharacterTestScenePath = "Assets/Scenes/CharacterTestScene.unity";

        [MenuItem("Tools/Rail/Create Stage 1")]
        public static void CreateStage1()
        {
            // 敵プレハブを用意(無ければCharacterTestSceneのEnemyから抽出)
            var enemyPrefab = EnsureEnemyPrefab();

            // 新しいシーンを作成して保存
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // --- メインルート(下) ---
            // x=28の崖の縁まで平坦。ジャンプでギャップ(3m)を跳び越えれば上ルート、
            // 跳ばなければそのまま谷(y=-1.9)へ落ちて下ルート(落ちること自体がルート選択)
            // 寸法はジャンプ性能(ギャップ限界4.5m/跳頂3.34m)の6〜7割を要求する1面相当
            // --- 1段目: 地上ルート ---
            // ①穴あき → ②丘(2段目への踏切)→ 2・3段目の下をくぐる → ③飛び石 → ゴール平地
            var main = CreateSpline("Stage1MainSpline", new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(14.0f, 0.0f, 0.0f),
                new Vector3(44.0f, 0.0f, 0.0f),
                new Vector3(47.5f, 1.5f, 0.0f),   // 丘(2段目への踏切)
                new Vector3(55.0f, 1.5f, 0.0f),   // 丘の縁(ここから前方3m・+2mを跳ぶ)
                new Vector3(58.0f, 0.0f, 0.0f),   // 2段目の下へ(頭上2.7m)
                new Vector3(80.0f, 0.0f, 0.0f),
                new Vector3(89.5f, 1.5f, 0.0f),   // 飛び石: 中
                new Vector3(97.5f, 3.0f, 0.0f),   // 飛び石: 高
                new Vector3(106.5f, 1.5f, 0.0f),  // 飛び石: 中
                new Vector3(115.5f, 0.0f, 0.0f),  // 飛び石: 低(着地)
                new Vector3(135.0f, 0.0f, 0.0f),  // ゴール平地
            });

            // --- 2段目 ---
            // 丘からジャンプで乗る。+z側(画面奥)へ膨らみながら進み、
            // 飛び石を丸ごとスキップして、かなり先(x=118)で1段目に端点合流する
            var middle = CreateSpline("Stage1MiddleSpline", new[]
            {
                new Vector3(58.0f, 3.5f, 0.0f),   // 入口(丘の縁から3m先・+2m)
                new Vector3(68.0f, 3.3f, 5.0f),
                new Vector3(82.0f, 2.5f, 9.0f),
                new Vector3(96.0f, 1.4f, 7.0f),
                new Vector3(110.0f, 0.4f, 3.0f),
                new Vector3(118.0f, 0.0f, 0.0f),  // 1段目に合流(端点ハンドオフ)
            });

            // --- 3段目(逆向きSpline) ---
            // 進行方向と反対向きに定義し、t=1.0側の端から乗る(合流に1.0側から乗る検証)。
            // 2段目の入口で振り返り、後ろ上方へジャンプして乗る。
            // 逆走方向へ+z側に曲がりながら高度を上げ、裏ゴールの高台で行き止まり
            var top = CreateSpline("Stage1TopSpline", new[]
            {
                new Vector3(22.0f, 7.5f, 18.0f),  // t=0: 裏ゴール(行き止まりの高台)
                new Vector3(28.0f, 7.2f, 10.0f),
                new Vector3(36.0f, 6.8f, 4.0f),
                new Vector3(44.0f, 6.3f, 0.0f),
                new Vector3(56.0f, 5.5f, 0.0f),   // t=1: 入口(2段目の入口から後ろ向きに2m・+2m)
            });

            Debug.Log("[Stage1Creator] Splineを生成しました。Stage Auto Builderを実行します。");

            // ステージ一式を生成(レール・ゲート・スポーン・マネージャ)。
            // 道は穴あき・すり抜け対応のため自前で敷くので、ビルダー側の道生成はOFF
            var window = EditorWindow.GetWindow<StageAutoBuilderWindow>("Stage Auto Builder");
            window.SetMainSpline(main);
            window.SetRoadEnabled(false);
            window.Build();

            var root = GameObject.Find("GeneratedStage");
            var parent = root != null ? root.transform : null;

            // --- 道を敷く(幅・厚みはジャンプ性能のスケール感に合わせる) ---
            // メイン道: ①の穴 + ③の飛び石の間に穴を開ける(railZ範囲)
            var mainHoles = new[]
            {
                new Vector2(16.0f, 18.0f),    // ① 2.0m
                new Vector2(24.0f, 26.5f),    // ① 2.5m
                new Vector2(33.0f, 36.0f),    // ① 3.0m
                new Vector2(84.5f, 87.5f),    // ③ 登り3m
                new Vector2(92.5f, 95.5f),    // ③ 登り3m
                new Vector2(101.0f, 105.0f),  // ③ 下り4m
                new Vector2(110.0f, 114.0f),  // ③ 下り4m
            };
            var mainRoad = StageGeometryUtility.LayCubes(main, 2.0f, 6.0f, 0.8f, true, parent, mainHoles);
            Undo.AddComponent<GeneratedStageContent>(mainRoad);

            // 2段目・3段目: 通常コリジョンの道(すり抜けは不使用。乗り口は互い違い配置で
            // 下の層の歩行帯に被さらないため、頭上の引っかかりは起きない)
            var middleRoad = StageGeometryUtility.LayCubes(middle, 2.0f, 6.0f, 0.8f, true, parent);
            Undo.AddComponent<GeneratedStageContent>(middleRoad);

            var topRoad = StageGeometryUtility.LayCubes(top, 2.0f, 6.0f, 0.8f, true, parent);
            Undo.AddComponent<GeneratedStageContent>(topRoad);

            // カメラはジャンプの放物線全体が見える距離まで引く
            var followCamera = Camera.main != null
                ? Camera.main.GetComponent<SplineFollowCamera>()
                : null;
            if (followCamera != null)
            {
                var cameraSo = new SerializedObject(followCamera);
                cameraSo.FindProperty("offset").vector3Value = new Vector3(0.0f, 3.0f, 14.0f);
                cameraSo.ApplyModifiedProperties();
            }

            // 敵を配置(地上序盤・ゴール手前、2段目に1体)
            if (enemyPrefab != null)
            {
                PlaceEnemy(enemyPrefab, main, 0.55f, parent);
                PlaceEnemy(enemyPrefab, main, 0.92f, parent);
                PlaceEnemy(enemyPrefab, middle, 0.5f, parent);
            }
            else
            {
                Debug.LogWarning("[Stage1Creator] 敵プレハブを用意できなかったため、敵の配置をスキップしました。");
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Stage1Creator] ステージ1を生成し、{ScenePath} に保存しました。");
        }

        static SplineContainer CreateSpline(string name, Vector3[] knots)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1 Spline");
            go.transform.position = Vector3.zero;

            var container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var knot in knots)
            {
                spline.Add(new BezierKnot((float3)knot), TangentMode.AutoSmooth);
            }

            return container;
        }

        static void PlaceEnemy(GameObject prefab, SplineContainer target, float t, Transform parent)
        {
            Vector3 pos = (Vector3)target.EvaluatePosition(t);
            Vector3 tan = ((Vector3)target.EvaluateTangent(t));
            tan.y = 0.0f;
            Quaternion rot = tan.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(tan.normalized)
                : Quaternion.identity;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;

            Undo.RegisterCreatedObjectUndo(instance, "Place Enemy");
            if (parent != null) instance.transform.SetParent(parent);
            // 道に少し浮かせて配置(CharacterControllerが接地させる)
            instance.transform.position = pos + Vector3.up * 0.5f;
            instance.transform.rotation = rot;

            var mover = instance.GetComponent<SplineMover>();
            if (mover != null)
            {
                var so = new SerializedObject(mover);
                so.FindProperty("splineContainer").objectReferenceValue = target;
                so.FindProperty("t").floatValue = t;
                so.ApplyModifiedProperties();
            }

            if (instance.GetComponent<GeneratedStageContent>() == null)
            {
                Undo.AddComponent<GeneratedStageContent>(instance);
            }
        }

        /// <summary>
        /// Enemy.prefabが無ければ、CharacterTestSceneのEnemyをプレハブ化して作る。
        /// </summary>
        static GameObject EnsureEnemyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (existing != null) return existing;

            if (!File.Exists(CharacterTestScenePath))
            {
                Debug.LogWarning($"[Stage1Creator] {CharacterTestScenePath} が見つかりません。");
                return null;
            }

            var scene = EditorSceneManager.OpenScene(CharacterTestScenePath, OpenSceneMode.Additive);
            GameObject prefab = null;

            try
            {
                GameObject enemySource = null;
                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var enemy = rootGo.GetComponentInChildren<EnemyController>(true);
                    if (enemy != null)
                    {
                        enemySource = enemy.gameObject;
                        break;
                    }
                }

                if (enemySource == null)
                {
                    Debug.LogWarning("[Stage1Creator] CharacterTestSceneにEnemyControllerが見つかりません。");
                    return null;
                }

                // 一時複製してシーン依存を切ってからプレハブ化
                var temp = Object.Instantiate(enemySource);
                temp.name = "Enemy";
                prefab = PrefabUtility.SaveAsPrefabAsset(temp, EnemyPrefabPath);
                Object.DestroyImmediate(temp);

                Debug.Log($"[Stage1Creator] {EnemyPrefabPath} を作成しました。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            return prefab;
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            Debug.Log("[Stage1Creator] 実行要求マーカーを検出しました。次のエディタ更新で実行します。");

            // InitializeOnLoad中のdelayCall登録は失われることがあるため、updateで自己解除方式にする
            EditorApplication.update += RunOnce;
        }

        static void RunOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(RequestMarkerPath))
            {
                EditorApplication.update -= RunOnce;
                return;
            }

            EditorApplication.update -= RunOnce;
            File.Delete(RequestMarkerPath);

            try
            {
                CreateStage1();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
