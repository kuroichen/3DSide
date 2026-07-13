using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// ジャンプ性能の計測用ジムステージを自動生成するメニュー。
    /// - ギャップ計測: 幅2.0〜5.0mの溝を順に跳ぶ(どこまで跳べるか)。落ちても1m下の底から跳んで復帰できる
    /// - 段差計測: 高さ0.5〜2.5mの台に順に跳び乗る(どこまで登れるか)
    /// jumpForce(CharacterStatus)やgravity(CharacterMover)を変えたら再生して体感するための場所。
    /// Library/create_jump_gym が存在する場合はコンパイル直後に一度だけ自動実行する(外部ツール連携用)。
    /// </summary>
    public static class JumpGymCreator
    {
        const string RequestMarkerPath = "Library/create_jump_gym";
        const string ScenePath = "Assets/Scenes/JumpGym.unity";
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        const float RoadWidth = 4.0f;

        // 現行ジャンププロファイル(高さ3.3m/滞空0.78秒/速度5)の実効値:
        // ギャップは理論3.9mにカプセル半径の得(+0.5m前後)、段差は跳頂3.34mにstepOffset(+0.3m)が乗る。
        // 実際の限界(ギャップ4.5m前後/段差3.6m前後)を跨ぐレンジにする
        static readonly float[] GapWidths = { 3.0f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f };
        static readonly float[] StepHeights = { 2.5f, 3.0f, 3.25f, 3.5f, 3.75f, 4.0f, 4.5f };

        [MenuItem("Tools/Rail/Create Jump Gym")]
        public static void CreateJumpGym()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var root = new GameObject("JumpGym");
            Undo.RegisterCreatedObjectUndo(root, "Create Jump Gym");

            // --- ギャップ計測コース ---
            // 床(上面y=0)を溝を空けながら並べる。溝の下には1m下に底があり、跳んで復帰できる
            CreateBlock(root.transform, 0.0f, 10.0f, 0.0f, -1.0f);

            var x = 10.0f;
            foreach (var gap in GapWidths)
            {
                CreateLabel(root.transform, new Vector3(x + gap * 0.5f, 1.8f, 0.0f), $"GAP {gap:0.0}m");
                x += gap;

                CreateBlock(root.transform, x, x + 6.0f, 0.0f, -1.0f);
                x += 6.0f;
            }

            // 溝の底(ギャップ区間全体の下)
            CreateBlock(root.transform, 8.0f, x, -1.0f, -2.0f);

            // --- 段差計測コース ---
            // 平地の上に高さ違いの台を並べる。台の間は5mの平地
            var stepStart = x;
            var sx = x + 5.0f;
            foreach (var height in StepHeights)
            {
                CreateBlock(root.transform, sx, sx + 6.0f, height, 0.0f);
                CreateLabel(root.transform, new Vector3(sx + 3.0f, height + 1.5f, 0.0f), $"STEP {height:0.00}m");
                sx += 6.0f + 5.0f;
            }

            var endX = sx + 5.0f;

            // 段差区間の地面
            CreateBlock(root.transform, stepStart, endX, 0.0f, -1.0f);

            // --- レール(直線) ---
            var splineGo = new GameObject("JumpGymSpline");
            Undo.RegisterCreatedObjectUndo(splineGo, "Create Jump Gym");
            var container = splineGo.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            spline.Add(new BezierKnot(new float3(0.0f, 0.0f, 0.0f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new float3(endX, 0.0f, 0.0f)), TangentMode.AutoSmooth);
            splineGo.AddComponent<SplineRail>();

            // --- スポーン地点 ---
            var spawnGo = new GameObject("SpawnPoint");
            Undo.RegisterCreatedObjectUndo(spawnGo, "Create Jump Gym");
            spawnGo.transform.SetParent(root.transform);
            var anchor = spawnGo.AddComponent<SplineAnchor>();
            var anchorSo = new SerializedObject(anchor);
            anchorSo.FindProperty("splineContainer").objectReferenceValue = container;
            anchorSo.FindProperty("t").floatValue = 2.0f / endX;
            anchorSo.FindProperty("direction").enumValueIndex = (int)SplineAnchorDirection.Forward;
            anchorSo.ApplyModifiedProperties();

            // --- カメラ・マネージャ ---
            var followCamera = Camera.main.gameObject.AddComponent<SplineFollowCamera>();
            var cameraSo = new SerializedObject(followCamera);
            cameraSo.FindProperty("offset").vector3Value = new Vector3(0.0f, 2.5f, 10.0f);
            cameraSo.ApplyModifiedProperties();

            new GameObject("RailManager").AddComponent<RailManager>();
            new GameObject("StageFlowManager").AddComponent<StageFlowManager>();

            var playerManager = new GameObject("PlayerManager").AddComponent<PlayerManager>();
            var managerSo = new SerializedObject(playerManager);
            managerSo.FindProperty("playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PlayerController>(PlayerPrefabPath);
            managerSo.FindProperty("spawnPoint").objectReferenceValue = anchor;
            managerSo.FindProperty("followCamera").objectReferenceValue = followCamera;
            managerSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[JumpGymCreator] ジムステージを生成し、{ScenePath} に保存しました。" +
                $"(ギャップ{GapWidths.Length}種 / 段差{StepHeights.Length}種 / 全長{endX:0}m)");
        }

        /// <summary>
        /// x範囲と上面・下面の高さを指定してブロックを置く。
        /// 壁ずり判定(CharacterMoverのCapsuleCast)が効くようTerrainレイヤーに載せる。
        /// </summary>
        static void CreateBlock(Transform parent, float fromX, float toX, float topY, float bottomY)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(block, "Create Jump Gym");
            block.transform.SetParent(parent);
            block.transform.position = new Vector3(
                (fromX + toX) * 0.5f,
                (topY + bottomY) * 0.5f,
                0.0f);
            block.transform.localScale = new Vector3(toX - fromX, topY - bottomY, RoadWidth);

            var terrainLayer = LayerMask.NameToLayer("Terrain");
            if (terrainLayer >= 0) block.layer = terrainLayer;
        }

        /// <summary>
        /// 計測値の表示ラベル。カメラ側(手前)に少し寄せて置く。
        /// </summary>
        static void CreateLabel(Transform parent, Vector3 position, string text)
        {
            var go = new GameObject($"Label_{text}");
            Undo.RegisterCreatedObjectUndo(go, "Create Jump Gym");
            go.transform.SetParent(parent);
            go.transform.position = position + Vector3.back * (RoadWidth * 0.5f + 0.2f);

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = textMesh.font.material;
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            Debug.Log("[JumpGymCreator] 実行要求マーカーを検出しました。次のエディタ更新で実行します。");

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
                CreateJumpGym();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
