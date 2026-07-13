using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Rail.EditorTools
{
    /// <summary>
    /// ステージ用Splineの「引く・分岐・連結」を補助するエディタツール。
    /// </summary>
    public class RailStageToolWindow : EditorWindow
    {
        // 新規Spline
        Vector3 newSplineStart = Vector3.zero;
        float newSplineLength = 10.0f;

        // パーツ追加
        SplineContainer partsTarget;
        float partLength = 10.0f;
        float partHeight = 3.0f;
        float partRadius = 5.0f;
        float partAmplitude = 3.0f;

        // 道メッシュ
        SplineContainer roadTarget;
        float roadWidth = 4.0f;
        int roadSamples = 50;

        // キューブ敷き
        SplineContainer cubeTarget;
        float cubeSpacing = 2.0f;
        float cubeWidth = 4.0f;
        float cubeThickness = 0.5f;
        bool cubeTiltToSlope = true;

        // 分岐
        SplineContainer branchSource;
        int branchKnotIndex = 0;
        Vector3 branchDirection = new Vector3(0.0f, 3.0f, 5.0f);

        // 連結
        SplineContainer mergeA;
        SplineContainer mergeB;

        // 地面スナップ
        SplineContainer snapTarget;
        float snapRayHeight = 50.0f;
        LayerMask snapGroundMask = ~0;

        Vector2 scroll;

        [MenuItem("Tools/Rail/Stage Spline Tool")]
        static void Open()
        {
            GetWindow<RailStageToolWindow>("Rail Stage Tool");
        }

        void OnGUI()
        {
            this.scroll = EditorGUILayout.BeginScrollView(this.scroll);

            DrawNewSplineSection();
            DrawSeparator();
            DrawPartsSection();
            DrawSeparator();
            DrawRoadMeshSection();
            DrawSeparator();
            DrawCubeRoadSection();
            DrawSeparator();
            DrawBranchSection();
            DrawSeparator();
            DrawMergeSection();
            DrawSeparator();
            DrawSnapSection();

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
        // 新規Spline
        // -----------------------------
        void DrawNewSplineSection()
        {
            EditorGUILayout.LabelField("① 新規Splineを引く", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "初期ノット2点付きのSplineを生成します。生成後はUnity標準のSpline編集ツールでクリックしてノットを追加・編集してください。",
                MessageType.None);

            this.newSplineStart = EditorGUILayout.Vector3Field("開始位置", this.newSplineStart);
            this.newSplineLength = EditorGUILayout.FloatField("初期の長さ(X方向)", this.newSplineLength);

            if (GUILayout.Button("新規Splineを生成"))
            {
                CreateNewSpline();
            }
        }

        void CreateNewSpline()
        {
            var go = new GameObject("StageSpline");
            Undo.RegisterCreatedObjectUndo(go, "Create Stage Spline");
            go.transform.position = Vector3.zero;

            var container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            spline.Add(new BezierKnot((float3)this.newSplineStart), TangentMode.AutoSmooth);
            spline.Add(
                new BezierKnot((float3)(this.newSplineStart + Vector3.right * this.newSplineLength)),
                TangentMode.AutoSmooth);

            Selection.activeGameObject = go;
            this.partsTarget = container;
        }

        // -----------------------------
        // パーツ追加
        // -----------------------------
        void DrawPartsSection()
        {
            EditorGUILayout.LabelField("①' パーツを継ぎ足す", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "対象Splineの末尾ノットから、選んだ形のノット列を継ぎ足します。進行方向は末尾2ノットから自動で判定します。",
                MessageType.None);

            this.partsTarget = (SplineContainer)EditorGUILayout.ObjectField(
                "対象Spline", this.partsTarget, typeof(SplineContainer), true);

            this.partLength = EditorGUILayout.FloatField("長さ", this.partLength);
            this.partHeight = EditorGUILayout.FloatField("高さ(坂)", this.partHeight);
            this.partRadius = EditorGUILayout.FloatField("半径(カーブ)", this.partRadius);
            this.partAmplitude = EditorGUILayout.FloatField("振れ幅(蛇行)", this.partAmplitude);

            using (new EditorGUI.DisabledScope(this.partsTarget == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("まっすぐ")) AppendPart(MakeStraight());
                if (GUILayout.Button("上り坂")) AppendPart(MakeSlope(1.0f));
                if (GUILayout.Button("下り坂")) AppendPart(MakeSlope(-1.0f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("90°右")) AppendPart(MakeTurn(90.0f, 1.0f));
                if (GUILayout.Button("90°左")) AppendPart(MakeTurn(90.0f, -1.0f));
                if (GUILayout.Button("Uターン")) AppendPart(MakeTurn(180.0f, 1.0f));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("蛇行道")) AppendPart(MakeZigzag());
            }
        }

        /// <summary>
        /// (forward, up, side) のオフセット列を末尾ノットから継ぎ足す。
        /// </summary>
        void AppendPart(List<Vector3> offsets)
        {
            var container = this.partsTarget;
            var spline = container.Spline;
            if (spline.Count == 0) return;

            Vector3 endPos = EndWorld(container);
            Vector3 fwd = EndHeading(container);
            Vector3 up = Vector3.up;
            Vector3 side = Vector3.Cross(up, fwd).normalized;

            Undo.RecordObject(container, "Append Part");

            foreach (var o in offsets)
            {
                Vector3 world = endPos + fwd * o.x + up * o.y + side * o.z;
                Vector3 local = container.transform.InverseTransformPoint(world);
                spline.Add(new BezierKnot((float3)local), TangentMode.AutoSmooth);
            }

            EditorUtility.SetDirty(container);
        }

        Vector3 EndWorld(SplineContainer c)
        {
            var s = c.Spline;
            var k = s[s.Count - 1];
            return c.transform.TransformPoint(k.Position);
        }

        Vector3 EndHeading(SplineContainer c)
        {
            var s = c.Spline;
            if (s.Count >= 2)
            {
                Vector3 a = c.transform.TransformPoint(s[s.Count - 2].Position);
                Vector3 b = c.transform.TransformPoint(s[s.Count - 1].Position);
                Vector3 d = b - a;
                d.y = 0.0f;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            return Vector3.right;
        }

        // --- パーツ定義（x=前, y=上, z=横）---

        List<Vector3> MakeStraight()
        {
            return new List<Vector3> { new Vector3(this.partLength, 0.0f, 0.0f) };
        }

        List<Vector3> MakeSlope(float sign)
        {
            float h = this.partHeight * sign;
            return new List<Vector3>
            {
                new Vector3(this.partLength * 0.5f, h * 0.5f, 0.0f),
                new Vector3(this.partLength, h, 0.0f),
            };
        }

        List<Vector3> MakeTurn(float degrees, float dir)
        {
            // dir: +1=右, -1=左。半径Rの円弧を一定間隔でサンプリング。
            var list = new List<Vector3>();
            int steps = Mathf.Max(4, Mathf.RoundToInt(degrees / 15.0f));
            float r = this.partRadius;
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Deg2Rad * (degrees * i / steps);
                float f = r * Mathf.Sin(a);
                float s = r * (1.0f - Mathf.Cos(a)) * dir;
                list.Add(new Vector3(f, 0.0f, s));
            }
            return list;
        }

        List<Vector3> MakeZigzag()
        {
            float L = this.partLength;
            float A = this.partAmplitude;
            return new List<Vector3>
            {
                new Vector3(L * 0.25f, 0.0f,  A),
                new Vector3(L * 0.50f, 0.0f,  0.0f),
                new Vector3(L * 0.75f, 0.0f, -A),
                new Vector3(L * 1.00f, 0.0f,  0.0f),
            };
        }

        // -----------------------------
        // 道メッシュ
        // -----------------------------
        void DrawRoadMeshSection()
        {
            EditorGUILayout.LabelField("①'' 道メッシュを生成", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Splineに沿って平らな道メッシュ(MeshColliderつき)を生成します。SplineExtrudeはチューブになるため、平らな道はこちらを使用。",
                MessageType.None);

            this.roadTarget = (SplineContainer)EditorGUILayout.ObjectField(
                "対象Spline", this.roadTarget, typeof(SplineContainer), true);
            this.roadWidth = EditorGUILayout.FloatField("道幅", this.roadWidth);
            this.roadSamples = EditorGUILayout.IntSlider("分割数", this.roadSamples, 4, 300);

            using (new EditorGUI.DisabledScope(this.roadTarget == null))
            {
                if (GUILayout.Button("道メッシュを生成"))
                {
                    GenerateRoadMesh();
                }
            }
        }

        void GenerateRoadMesh()
        {
            var go = StageGeometryUtility.GenerateRoadMesh(
                this.roadTarget, this.roadWidth, this.roadSamples, null);
            Selection.activeGameObject = go;
        }

        // -----------------------------
        // キューブ敷き（ブロックアウト）
        // -----------------------------
        void DrawCubeRoadSection()
        {
            EditorGUILayout.LabelField("①''' キューブで道を敷く（ブロックアウト）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Splineに沿ってキューブを並べます。各キューブがBoxColliderを持つので即歩けます。プロトタイプ向け。",
                MessageType.None);

            this.cubeTarget = (SplineContainer)EditorGUILayout.ObjectField(
                "対象Spline", this.cubeTarget, typeof(SplineContainer), true);
            this.cubeSpacing = EditorGUILayout.FloatField("間隔", this.cubeSpacing);
            this.cubeWidth = EditorGUILayout.FloatField("道幅", this.cubeWidth);
            this.cubeThickness = EditorGUILayout.FloatField("厚み", this.cubeThickness);
            this.cubeTiltToSlope = EditorGUILayout.Toggle("坂に沿って傾ける", this.cubeTiltToSlope);

            using (new EditorGUI.DisabledScope(this.cubeTarget == null))
            {
                if (GUILayout.Button("キューブで道を敷く"))
                {
                    LayCubes();
                }
            }
        }

        void LayCubes()
        {
            var go = StageGeometryUtility.LayCubes(
                this.cubeTarget, this.cubeSpacing, this.cubeWidth,
                this.cubeThickness, this.cubeTiltToSlope, null);
            Selection.activeGameObject = go;
        }

        // -----------------------------
        // 分岐
        // -----------------------------
        void DrawBranchSection()
        {
            EditorGUILayout.LabelField("② 分岐を作成", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "元Splineの指定ノットから分岐Splineを生成し、切り替え用のSplineGate(トリガーColliderつき)を配置・配線します。",
                MessageType.None);

            this.branchSource = (SplineContainer)EditorGUILayout.ObjectField(
                "分岐元Spline", this.branchSource, typeof(SplineContainer), true);

            if (this.branchSource != null)
            {
                int max = Mathf.Max(0, this.branchSource.Spline.Count - 1);
                this.branchKnotIndex = EditorGUILayout.IntSlider(
                    "分岐ノット番号", this.branchKnotIndex, 0, max);
            }

            this.branchDirection = EditorGUILayout.Vector3Field(
                "分岐の初期方向(相対)", this.branchDirection);

            using (new EditorGUI.DisabledScope(this.branchSource == null))
            {
                if (GUILayout.Button("分岐を作成"))
                {
                    CreateBranch();
                }
            }
        }

        void CreateBranch()
        {
            var sourceSpline = this.branchSource.Spline;
            int index = Mathf.Clamp(this.branchKnotIndex, 0, sourceSpline.Count - 1);

            // 分岐点のワールド座標
            var knot = sourceSpline[index];
            Vector3 branchWorld = this.branchSource.transform.TransformPoint(knot.Position);

            // 分岐点での接線(向き判定・ゲート向きに使用)
            float t = sourceSpline.Count > 1 ? (float)index / (sourceSpline.Count - 1) : 0.0f;
            Vector3 tangent = ((Vector3)this.branchSource.EvaluateTangent(t));
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
            tangent.Normalize();

            // --- 分岐Spline生成(原点・identityでlocal==world) ---
            var branchGo = new GameObject("BranchSpline");
            Undo.RegisterCreatedObjectUndo(branchGo, "Create Branch");
            var branchContainer = branchGo.AddComponent<SplineContainer>();
            var branchSpline = branchContainer.Spline;
            branchSpline.Clear();
            branchSpline.Add(new BezierKnot((float3)branchWorld), TangentMode.AutoSmooth);
            branchSpline.Add(
                new BezierKnot((float3)(branchWorld + this.branchDirection)),
                TangentMode.AutoSmooth);

            // --- Gate生成 ---
            var gateGo = new GameObject("SplineGate");
            Undo.RegisterCreatedObjectUndo(gateGo, "Create Gate");
            gateGo.transform.position = branchWorld;
            gateGo.transform.rotation = Quaternion.LookRotation(tangent);

            int gateLayer = LayerMask.NameToLayer("Gate");
            if (gateLayer >= 0) gateGo.layer = gateLayer;

            var box = gateGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.0f, 4.0f, 0.2f);
            box.center = new Vector3(0.0f, 2.0f, 0.0f);

            var gate = gateGo.AddComponent<SplineGate>();

            // SplineGateのprivate [SerializeField] を配線
            var so = new SerializedObject(gate);
            so.FindProperty("splineOnFront").objectReferenceValue = this.branchSource;
            so.FindProperty("splineOnBack").objectReferenceValue = branchContainer;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = branchGo;

            if (gateLayer < 0)
            {
                Debug.LogWarning("[RailStageTool] \"Gate\"レイヤーが見つかりません。Tags & Layersに追加してGateのlayerを設定してください。");
            }
        }

        // -----------------------------
        // 連結
        // -----------------------------
        void DrawMergeSection()
        {
            EditorGUILayout.LabelField("③ 連結(MovementSpline生成)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "2本のSplineを1本に連結したMovementSplineを生成します。見た目用の元Splineはそのまま残ります。",
                MessageType.None);

            this.mergeA = (SplineContainer)EditorGUILayout.ObjectField(
                "Spline A (前)", this.mergeA, typeof(SplineContainer), true);
            this.mergeB = (SplineContainer)EditorGUILayout.ObjectField(
                "Spline B (後)", this.mergeB, typeof(SplineContainer), true);

            using (new EditorGUI.DisabledScope(this.mergeA == null || this.mergeB == null))
            {
                if (GUILayout.Button("連結Splineを生成"))
                {
                    Merge();
                }
            }
        }

        void Merge()
        {
            var go = new GameObject("MovementSpline");
            Undo.RegisterCreatedObjectUndo(go, "Create Movement Spline");

            var container = go.AddComponent<SplineContainer>();
            var merged = container.Spline;
            merged.Clear();

            AppendKnots(merged, container.transform, this.mergeA);
            AppendKnots(merged, container.transform, this.mergeB);

            Selection.activeGameObject = go;
        }

        void AppendKnots(Spline target, Transform targetTransform, SplineContainer source)
        {
            foreach (var knot in source.Spline)
            {
                // 元ローカル → ワールド → 連結先ローカル
                float3 worldPos = source.transform.TransformPoint(knot.Position);
                float3 localPos = targetTransform.InverseTransformPoint(worldPos);
                target.Add(new BezierKnot(localPos), TangentMode.AutoSmooth);
            }
        }

        // -----------------------------
        // 地面スナップ
        // -----------------------------
        void DrawSnapSection()
        {
            EditorGUILayout.LabelField("④ ノットを地面にスナップ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "各ノットの真上からレイを飛ばし、当たった地面の高さにY座標を合わせます。",
                MessageType.None);

            this.snapTarget = (SplineContainer)EditorGUILayout.ObjectField(
                "対象Spline", this.snapTarget, typeof(SplineContainer), true);
            this.snapRayHeight = EditorGUILayout.FloatField("レイの開始高さ", this.snapRayHeight);

            using (new EditorGUI.DisabledScope(this.snapTarget == null))
            {
                if (GUILayout.Button("地面にスナップ"))
                {
                    SnapToGround();
                }
            }
        }

        void SnapToGround()
        {
            var spline = this.snapTarget.Spline;
            var tr = this.snapTarget.transform;

            Undo.RecordObject(this.snapTarget, "Snap Knots To Ground");

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                Vector3 world = tr.TransformPoint(knot.Position);
                Vector3 origin = new Vector3(world.x, world.y + this.snapRayHeight, world.z);

                if (Physics.Raycast(origin, Vector3.down, out var hit, this.snapRayHeight * 2.0f, this.snapGroundMask))
                {
                    Vector3 snapped = new Vector3(world.x, hit.point.y, world.z);
                    Vector3 local = tr.InverseTransformPoint(snapped);
                    knot.Position = (float3)local;
                    spline[i] = knot;
                }
            }

            EditorUtility.SetDirty(this.snapTarget);
        }
    }
}
