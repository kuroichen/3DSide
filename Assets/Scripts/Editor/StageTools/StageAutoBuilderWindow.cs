using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// Splineから遊べるステージ(レール・道・ゲート・スポーン地点・マネージャ・敵/アイテム)を
    /// 1クリックで自動生成するエディタツール。
    /// 生成物はすべて「GeneratedStage」ルート配下にまとめ、再生成時に安全に作り直す(冪等)。
    /// </summary>
    public class StageAutoBuilderWindow : EditorWindow
    {
        enum RoadMode
        {
            Mesh,   // 道メッシュ
            Cube,   // キューブ敷き
            None,   // なし
        }

        /// <summary>敵・アイテムの配置1件分。</summary>
        [System.Serializable]
        class PlacementEntry
        {
            public GameObject prefab = null;
            public int count = 3;
            public float tMin = 0.1f;
            public float tMax = 0.9f;
        }

        const string RootName = "GeneratedStage";
        const string UndoName = "Stage Auto Build";

        // 対象Spline
        bool targetAllSplines = true;
        SplineContainer mainSpline = null;

        // ① SplineRail付与
        bool enableSplineRail = true;
        float maxHorizontalOffset = 2.0f;
        float maxVerticalOffset = 5.0f;

        // ② 道ジオメトリ
        bool enableRoad = true;
        RoadMode roadMode = RoadMode.Cube;
        float roadWidth = 4.0f;
        int roadSamples = 50;
        float cubeSpacing = 2.0f;
        float cubeWidth = 4.0f;
        float cubeThickness = 0.5f;
        bool cubeTiltToSlope = true;

        // ③ 分岐ゲート自動検出
        bool enableGate = true;
        float gateDistanceThreshold = 0.5f;

        // ④ スポーン地点
        bool enableSpawn = true;

        // ⑤ マネージャ配線
        bool enableManagers = true;
        Vector3 cameraOffset = new Vector3(0.0f, 2.0f, 8.0f);

        // ⑥ 敵・アイテム配置
        bool enablePlacement = true;
        List<PlacementEntry> placements = new();

        Vector2 scroll;

        [MenuItem("Tools/Rail/Stage Auto Builder")]
        static void Open()
        {
            GetWindow<StageAutoBuilderWindow>("Stage Auto Builder");
        }

        void OnGUI()
        {
            this.scroll = EditorGUILayout.BeginScrollView(this.scroll);

            DrawTargetSection();
            DrawSeparator();
            DrawSplineRailSection();
            DrawSeparator();
            DrawRoadSection();
            DrawSeparator();
            DrawGateSection();
            DrawSeparator();
            DrawSpawnSection();
            DrawSeparator();
            DrawManagerSection();
            DrawSeparator();
            DrawPlacementSection();
            DrawSeparator();
            DrawBuildButton();

            EditorGUILayout.EndScrollView();
        }

        void DrawSeparator()
        {
            EditorGUILayout.Space();
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space();
        }

        // -----------------------------
        // 対象Spline
        // -----------------------------
        void DrawTargetSection()
        {
            EditorGUILayout.LabelField("対象Spline", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "処理対象のSplineを決めます。「全SplineContainerを対象」ONでシーン内のすべてのSplineを対象にします。" +
                "メインSplineはスポーン地点を置く基準Splineです(未指定なら対象リストの先頭)。",
                MessageType.None);

            this.targetAllSplines = EditorGUILayout.Toggle(
                "シーン内の全SplineContainerを対象", this.targetAllSplines);
            this.mainSpline = (SplineContainer)EditorGUILayout.ObjectField(
                "メインSpline", this.mainSpline, typeof(SplineContainer), true);
        }

        // -----------------------------
        // ① SplineRail付与
        // -----------------------------
        void DrawSplineRailSection()
        {
            this.enableSplineRail = EditorGUILayout.ToggleLeft(
                "① SplineRailを付与", this.enableSplineRail, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "各対象SplineにSplineRailを付与します。既にある場合はオフセット値だけ更新します。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!this.enableSplineRail))
            {
                this.maxHorizontalOffset = EditorGUILayout.FloatField(
                    "最大水平オフセット", this.maxHorizontalOffset);
                this.maxVerticalOffset = EditorGUILayout.FloatField(
                    "最大垂直オフセット", this.maxVerticalOffset);
            }
        }

        // -----------------------------
        // ② 道ジオメトリ
        // -----------------------------
        void DrawRoadSection()
        {
            this.enableRoad = EditorGUILayout.ToggleLeft(
                "② 道ジオメトリを生成", this.enableRoad, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "各対象Splineに沿って道を生成します。メッシュ(平らな道)かキューブ(ブロックアウト)を選べます。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!this.enableRoad))
            {
                this.roadMode = (RoadMode)EditorGUILayout.EnumPopup("モード", this.roadMode);

                if (this.roadMode == RoadMode.Mesh)
                {
                    this.roadWidth = EditorGUILayout.FloatField("道幅", this.roadWidth);
                    this.roadSamples = EditorGUILayout.IntSlider("分割数", this.roadSamples, 4, 300);
                }
                else if (this.roadMode == RoadMode.Cube)
                {
                    this.cubeSpacing = EditorGUILayout.FloatField("間隔", this.cubeSpacing);
                    this.cubeWidth = EditorGUILayout.FloatField("道幅", this.cubeWidth);
                    this.cubeThickness = EditorGUILayout.FloatField("厚み", this.cubeThickness);
                    this.cubeTiltToSlope = EditorGUILayout.Toggle("坂に沿って傾ける", this.cubeTiltToSlope);
                }
            }
        }

        // -----------------------------
        // ③ 分岐ゲート自動検出
        // -----------------------------
        void DrawGateSection()
        {
            this.enableGate = EditorGUILayout.ToggleLeft(
                "③ 分岐ゲートを自動検出", this.enableGate, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "あるSpline Bの始点が別Spline Aの途中に乗っている(分岐している)地点を検出し、" +
                "切り替え用のSplineGate(トリガーColliderつき)を自動配置します。端点同士の単純連結はスキップします。" +
                "\"Gate\"レイヤーが必要です。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!this.enableGate))
            {
                this.gateDistanceThreshold = EditorGUILayout.FloatField(
                    "距離しきい値(m)", this.gateDistanceThreshold);
            }
        }

        // -----------------------------
        // ④ スポーン地点
        // -----------------------------
        void DrawSpawnSection()
        {
            this.enableSpawn = EditorGUILayout.ToggleLeft(
                "④ スポーン地点を生成", this.enableSpawn, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "メインSplineのt=0にSplineAnchorつきの「SpawnPoint」を生成します。",
                MessageType.None);
        }

        // -----------------------------
        // ⑤ マネージャ配線
        // -----------------------------
        void DrawManagerSection()
        {
            this.enableManagers = EditorGUILayout.ToggleLeft(
                "⑤ マネージャを生成・配線", this.enableManagers, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "RailManager / StageFlowManager と、カメラ(SplineFollowCamera)を用意します。" +
                "MainCameraがあればそこにカメラ機能を追加し、無ければStageCameraを新規生成します。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!this.enableManagers))
            {
                this.cameraOffset = EditorGUILayout.Vector3Field("カメラオフセット", this.cameraOffset);
            }
        }

        // -----------------------------
        // ⑥ 敵・アイテム配置
        // -----------------------------
        void DrawPlacementSection()
        {
            this.enablePlacement = EditorGUILayout.ToggleLeft(
                "⑥ 敵・アイテムを配置", this.enablePlacement, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "指定したプレハブを各対象Splineに等間隔で配置します。SplineMover派生ならSplineとtを自動配線します。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!this.enablePlacement))
            {
                for (int i = 0; i < this.placements.Count; i++)
                {
                    var entry = this.placements[i];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"配置 {i + 1}", EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        this.placements.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                        "プレハブ", entry.prefab, typeof(GameObject), false);
                    entry.count = Mathf.Max(0, EditorGUILayout.IntField("個数", entry.count));
                    EditorGUILayout.BeginHorizontal();
                    entry.tMin = EditorGUILayout.FloatField("t min", entry.tMin);
                    entry.tMax = EditorGUILayout.FloatField("t max", entry.tMax);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                if (GUILayout.Button("配置エントリを追加"))
                {
                    this.placements.Add(new PlacementEntry());
                }
            }
        }

        // -----------------------------
        // ステージ生成ボタン
        // -----------------------------
        void DrawBuildButton()
        {
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("ステージ生成", GUILayout.Height(36)))
                {
                    Build();
                }
            }
        }

        /// <summary>
        /// スポーン地点を置くメインSplineを外部から指定する(スクリプト連携用)。
        /// </summary>
        public void SetMainSpline(SplineContainer spline)
        {
            this.mainSpline = spline;
        }

        /// <summary>
        /// キューブ道の生成設定を外部から指定する(スクリプト連携用)。
        /// </summary>
        public void SetCubeRoadSettings(float spacing, float width, float thickness, bool tiltToSlope)
        {
            this.roadMode = RoadMode.Cube;
            this.cubeSpacing = spacing;
            this.cubeWidth = width;
            this.cubeThickness = thickness;
            this.cubeTiltToSlope = tiltToSlope;
        }

        /// <summary>
        /// 道ジオメトリ生成のON/OFFを外部から指定する(スクリプト連携用)。
        /// 穴あき道など、呼び出し側が自前で道を敷きたい場合はOFFにする。
        /// </summary>
        public void SetRoadEnabled(bool enabled)
        {
            this.enableRoad = enabled;
        }

        // =============================
        // 生成本体
        // =============================
        public void Build()
        {
            var targets = CollectTargetSplines();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[StageAutoBuilder] 対象のSplineContainerが見つかりません。");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            int group = Undo.GetCurrentGroup();

            // 既存の生成物を削除(冪等)
            RemoveExistingGenerated();

            // ルート生成
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            root.transform.position = Vector3.zero;
            Undo.AddComponent<GeneratedStageContent>(root);

            var mainTarget = ResolveMainSpline(targets);
            var log = new StringBuilder();
            log.AppendLine("[StageAutoBuilder] ステージ生成完了:");
            log.AppendLine($"- 対象Spline数: {targets.Count} / メインSpline: {(mainTarget != null ? mainTarget.name : "なし")}");

            if (this.enableSplineRail)
            {
                int n = BuildSplineRails(targets);
                log.AppendLine($"- SplineRail付与: {n}本");
            }

            if (this.enableRoad && this.roadMode != RoadMode.None)
            {
                int n = BuildRoads(targets, root.transform);
                log.AppendLine($"- 道ジオメトリ({this.roadMode}): {n}件");
            }

            if (this.enableGate)
            {
                int n = BuildGates(targets, root.transform);
                log.AppendLine($"- 分岐ゲート: {n}個");
            }

            SplineAnchor spawnAnchor = null;
            if (this.enableSpawn && mainTarget != null)
            {
                spawnAnchor = BuildSpawnPoint(mainTarget, root.transform);
                log.AppendLine("- スポーン地点: 生成");
            }

            if (this.enableManagers)
            {
                BuildManagers(root.transform, log);
            }

            if (this.enablePlacement)
            {
                int n = BuildPlacements(targets, root.transform);
                log.AppendLine($"- 敵・アイテム配置: {n}体");
            }

            Selection.activeGameObject = root;
            Undo.CollapseUndoOperations(group);

            Debug.Log(log.ToString());
        }

        // -----------------------------
        // 対象Spline収集
        // -----------------------------
        List<SplineContainer> CollectTargetSplines()
        {
            var result = new List<SplineContainer>();

            if (this.targetAllSplines)
            {
                var all = FindObjectsByType<SplineContainer>(FindObjectsInactive.Include);
                foreach (var c in all)
                {
                    if (c != null && !result.Contains(c)) result.Add(c);
                }
            }
            else if (this.mainSpline != null)
            {
                result.Add(this.mainSpline);
                var all = FindObjectsByType<SplineContainer>(FindObjectsInactive.Include);
                foreach (var c in all)
                {
                    if (c != null && !result.Contains(c)) result.Add(c);
                }
            }

            return result;
        }

        SplineContainer ResolveMainSpline(List<SplineContainer> targets)
        {
            if (this.mainSpline != null) return this.mainSpline;
            return targets.Count > 0 ? targets[0] : null;
        }

        // -----------------------------
        // ① SplineRail
        // -----------------------------
        int BuildSplineRails(List<SplineContainer> targets)
        {
            int count = 0;
            foreach (var target in targets)
            {
                var rail = target.GetComponent<SplineRail>();
                if (rail == null)
                {
                    rail = Undo.AddComponent<SplineRail>(target.gameObject);
                }

                var so = new SerializedObject(rail);
                so.FindProperty("splineContainer").objectReferenceValue = target;
                so.FindProperty("maxHorizontalOffset").floatValue = this.maxHorizontalOffset;
                so.FindProperty("maxVerticalOffset").floatValue = this.maxVerticalOffset;
                so.ApplyModifiedProperties();
                count++;
            }
            return count;
        }

        // -----------------------------
        // ② 道ジオメトリ
        // -----------------------------
        int BuildRoads(List<SplineContainer> targets, Transform parent)
        {
            int count = 0;
            foreach (var target in targets)
            {
                if (target.Spline == null || target.Spline.Count < 2) continue;

                GameObject go = this.roadMode == RoadMode.Mesh
                    ? StageGeometryUtility.GenerateRoadMesh(
                        target, this.roadWidth, this.roadSamples, parent)
                    : StageGeometryUtility.LayCubes(
                        target, this.cubeSpacing, this.cubeWidth,
                        this.cubeThickness, this.cubeTiltToSlope, parent);

                if (go != null)
                {
                    Undo.AddComponent<GeneratedStageContent>(go);
                    count++;
                }
            }
            return count;
        }

        // -----------------------------
        // ③ 分岐ゲート自動検出
        // -----------------------------
        int BuildGates(List<SplineContainer> targets, Transform parent)
        {
            const float endpointTolerance = 0.02f;

            bool gateLayerMissing = false;
            int count = 0;

            foreach (var b in targets)
            {
                var bSpline = b.Spline;
                if (bSpline == null || bSpline.Count == 0) continue;

                // Bの始点(最初のknot)のワールド座標
                Vector3 bStartWorld = b.transform.TransformPoint((Vector3)bSpline[0].Position);

                foreach (var a in targets)
                {
                    if (a == b) continue;
                    var aSpline = a.Spline;
                    if (aSpline == null || aSpline.Count < 2) continue;

                    // A上の最近傍点を求める
                    var localQuery = a.transform.InverseTransformPoint(bStartWorld);
                    SplineUtility.GetNearestPoint(
                        aSpline, (float3)localQuery, out float3 nearestLocal, out float tA);

                    Vector3 nearestWorld = a.transform.TransformPoint((Vector3)nearestLocal);
                    float dist = Vector3.Distance(bStartWorld, nearestWorld);
                    if (dist > this.gateDistanceThreshold) continue;

                    // A-Bの端点同士が一致しているだけの単純連結はスキップ
                    if (tA <= endpointTolerance || tA >= 1.0f - endpointTolerance) continue;

                    Vector3 tangent = ((Vector3)a.EvaluateTangent(tA));
                    if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
                    tangent.Normalize();

                    if (CreateGate(a, b, nearestWorld, tangent, parent)) count++;
                    else gateLayerMissing = true;
                }
            }

            if (gateLayerMissing)
            {
                Debug.LogWarning("[StageAutoBuilder] \"Gate\"レイヤーが見つかりません。" +
                    "Tags & Layersに\"Gate\"を追加してください。");
            }

            return count;
        }

        bool CreateGate(
            SplineContainer front, SplineContainer back,
            Vector3 position, Vector3 tangent, Transform parent)
        {
            var gateGo = new GameObject("SplineGate");
            Undo.RegisterCreatedObjectUndo(gateGo, UndoName);
            gateGo.transform.SetParent(parent);
            gateGo.transform.position = position;
            gateGo.transform.rotation = Quaternion.LookRotation(tangent);

            int gateLayer = LayerMask.NameToLayer("Gate");
            bool hasLayer = gateLayer >= 0;
            if (hasLayer) gateGo.layer = gateLayer;

            var box = Undo.AddComponent<BoxCollider>(gateGo);
            box.isTrigger = true;
            box.size = new Vector3(3.0f, 4.0f, 0.2f);
            box.center = new Vector3(0.0f, 2.0f, 0.0f);

            var gate = Undo.AddComponent<SplineGate>(gateGo);
            var so = new SerializedObject(gate);
            so.FindProperty("splineOnFront").objectReferenceValue = front;
            so.FindProperty("splineOnBack").objectReferenceValue = back;
            so.ApplyModifiedProperties();

            Undo.AddComponent<GeneratedStageContent>(gateGo);
            return hasLayer;
        }

        // -----------------------------
        // ④ スポーン地点
        // -----------------------------
        SplineAnchor BuildSpawnPoint(SplineContainer mainTarget, Transform parent)
        {
            var spawnGo = new GameObject("SpawnPoint");
            Undo.RegisterCreatedObjectUndo(spawnGo, UndoName);
            spawnGo.transform.SetParent(parent);
            spawnGo.transform.position = (Vector3)mainTarget.EvaluatePosition(0.0f);

            var anchor = Undo.AddComponent<SplineAnchor>(spawnGo);
            var so = new SerializedObject(anchor);
            so.FindProperty("splineContainer").objectReferenceValue = mainTarget;
            so.FindProperty("t").floatValue = 0.0f;
            so.FindProperty("direction").enumValueIndex = (int)SplineAnchorDirection.Forward;
            so.ApplyModifiedProperties();

            Undo.AddComponent<GeneratedStageContent>(spawnGo);
            return anchor;
        }

        // -----------------------------
        // ⑤ マネージャ配線
        // -----------------------------
        void BuildManagers(Transform parent, StringBuilder log)
        {
            // RailManager
            if (FindAnyObjectByType<RailManager>() == null)
            {
                CreateManagerObject<RailManager>("RailManager", parent);
                log.AppendLine("- RailManager: 生成");
            }

            // StageFlowManager
            var flowManager = FindAnyObjectByType<StageFlowManager>();
            if (flowManager == null)
            {
                flowManager = CreateManagerObject<StageFlowManager>("StageFlowManager", parent);
                log.AppendLine("- StageFlowManager: 生成");
            }

            // カメラ
            ResolveFollowCamera(parent, log);

            // 個別ステージSceneのUnity表現
            var stageView = parent.GetComponent<StageView>();
            if (stageView == null)
            {
                stageView = Undo.AddComponent<StageView>(parent.gameObject);
                log.AppendLine("- StageView: 生成");
            }

        }

        T CreateManagerObject<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            go.transform.SetParent(parent);
            var component = Undo.AddComponent<T>(go);
            Undo.AddComponent<GeneratedStageContent>(go);
            return component;
        }

        SplineFollowCamera ResolveFollowCamera(Transform parent, StringBuilder log)
        {
            SplineFollowCamera followCamera;

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                // 既存のMainCameraに追加(ルート下には入れず、再生成でも削除しない)
                followCamera = mainCam.GetComponent<SplineFollowCamera>();
                if (followCamera == null)
                {
                    followCamera = Undo.AddComponent<SplineFollowCamera>(mainCam.gameObject);
                    log.AppendLine("- カメラ: MainCameraにSplineFollowCameraを追加");
                }
            }
            else
            {
                var camGo = new GameObject("StageCamera");
                Undo.RegisterCreatedObjectUndo(camGo, UndoName);
                camGo.transform.SetParent(parent);
                Undo.AddComponent<Camera>(camGo);
                followCamera = Undo.AddComponent<SplineFollowCamera>(camGo);
                Undo.AddComponent<GeneratedStageContent>(camGo);
                log.AppendLine("- カメラ: StageCameraを新規生成");
            }

            var camSo = new SerializedObject(followCamera);
            camSo.FindProperty("offset").vector3Value = this.cameraOffset;
            camSo.ApplyModifiedProperties();

            return followCamera;
        }

        // -----------------------------
        // ⑥ 敵・アイテム配置
        // -----------------------------
        int BuildPlacements(List<SplineContainer> targets, Transform parent)
        {
            int count = 0;
            foreach (var entry in this.placements)
            {
                if (entry.prefab == null || entry.count <= 0) continue;

                foreach (var target in targets)
                {
                    if (target.Spline == null || target.Spline.Count < 2) continue;

                    for (int i = 0; i < entry.count; i++)
                    {
                        float t = entry.count == 1
                            ? (entry.tMin + entry.tMax) * 0.5f
                            : Mathf.Lerp(entry.tMin, entry.tMax, (float)i / (entry.count - 1));
                        t = Mathf.Clamp01(t);

                        Vector3 pos = (Vector3)target.EvaluatePosition(t);
                        Vector3 tan = ((Vector3)target.EvaluateTangent(t));
                        tan.y = 0.0f;
                        Quaternion rot = tan.sqrMagnitude > 0.0001f
                            ? Quaternion.LookRotation(tan.normalized)
                            : Quaternion.identity;

                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);
                        if (instance == null) continue;

                        Undo.RegisterCreatedObjectUndo(instance, UndoName);
                        instance.transform.SetParent(parent);
                        instance.transform.position = pos;
                        instance.transform.rotation = rot;

                        var mover = instance.GetComponent<SplineMover>();
                        if (mover != null)
                        {
                            var so = new SerializedObject(mover);
                            so.FindProperty("splineContainer").objectReferenceValue = target;
                            so.FindProperty("t").floatValue = t;
                            so.ApplyModifiedProperties();
                        }

                        Undo.AddComponent<GeneratedStageContent>(instance);
                        count++;
                    }
                }
            }
            return count;
        }

        // -----------------------------
        // 既存生成物の削除(冪等)
        // -----------------------------
        void RemoveExistingGenerated()
        {
            var all = FindObjectsByType<GeneratedStageContent>(FindObjectsInactive.Include);

            foreach (var content in all)
            {
                if (content == null) continue;
                // 生成物ルート(祖先に生成物マーカーが無いもの)だけを削除すれば子は連鎖削除される
                if (HasGeneratedAncestor(content.transform)) continue;
                Undo.DestroyObjectImmediate(content.gameObject);
            }
        }

        bool HasGeneratedAncestor(Transform transform)
        {
            var parent = transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<GeneratedStageContent>() != null) return true;
                parent = parent.parent;
            }
            return false;
        }
    }
}
