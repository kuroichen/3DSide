using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// 2ステージ目を自動生成するメニュー。
    /// 奥行き方向にS字でうねる平坦な1本道と、終点手前のゴールのみ(穴・分岐・敵なし)。
    /// 単一エリアの開始地点とイベントをStageDefinitionへ配線する。
    /// </summary>
    public static class Stage2Creator
    {
        const string ScenePath = "Assets/Scenes/World1_Stage2.unity";

        // ゴールを置く位置(終点から手前に何mか)
        const float GoalDistanceFromEnd = 8.0f;

        [MenuItem("Tools/Rail/Create Stage 2")]
        public static void CreateStage2()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // 奥行き方向(z)にS字でうねる平坦な1本道
            var main = CreateSpline("Stage2MainSpline", new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(15.0f, 0.0f, 0.0f),
                new Vector3(30.0f, 0.0f, 8.0f),
                new Vector3(45.0f, 0.0f, 12.0f),
                new Vector3(60.0f, 0.0f, 6.0f),
                new Vector3(75.0f, 0.0f, -4.0f),
                new Vector3(90.0f, 0.0f, 0.0f),
                new Vector3(105.0f, 0.0f, 0.0f),
            });

            Debug.Log("[Stage2Creator] Splineを生成しました。Stage Auto Builderを実行します。");

            // レール・道・スポーン・マネージャ一式を生成
            var window = EditorWindow.GetWindow<StageAutoBuilderWindow>("Stage Auto Builder");
            window.SetMainSpline(main);
            window.SetRoadEnabled(true);
            window.Build();

            var root = GameObject.Find("GeneratedStage");
            var rootTransform = root != null ? root.transform : null;
            main.transform.SetParent(rootTransform);

            // --- 見た目(FantasyForestがあれば優先、無ければ既定の緑の丘) ---
            var theme = FantasyThemeSetupTool.TryLoadTheme();
            if (theme == null)
            {
                theme = ThemeSetupTool.EnsureDefaultTheme();
            }
            if (theme != null)
            {
                StageDecorator.Scatter(main, theme, rootTransform);
                StageDecorator.ApplyEnvironment(theme);
            }

            // --- ゴール(終点の手前。カーブがあるのでxではなく距離で求める) ---
            float length = main.CalculateLength();
            main.Spline.GetPointAtLinearDistance(0.0f, length - GoalDistanceFromEnd, out float goalT);

            var goalTrigger = CreateEventTrigger(
                "Goal", main, goalT, new Vector3(6.0f, 4.0f, 0.5f), rootTransform);
            WireTriggerInfo(goalTrigger, Undo.AddComponent<GoalInfo>(goalTrigger.gameObject));

            var triggers = new List<AreaTrigger> { goalTrigger };

            // --- ステージ定義と配線 ---
            var definitionGo = new GameObject("StageDefinition");
            Undo.RegisterCreatedObjectUndo(definitionGo, "Create Stage2");
            definitionGo.transform.SetParent(rootTransform);
            var definition = Undo.AddComponent<StageDefinition>(definitionGo);

            var spawnPointGo = GameObject.Find("SpawnPoint");
            var spawnAnchor = spawnPointGo != null ? spawnPointGo.GetComponent<SplineAnchor>() : null;
            var followCamera = Object.FindAnyObjectByType<SplineFollowCamera>();

            var areaGo = new GameObject("Area1");
            Undo.RegisterCreatedObjectUndo(areaGo, "Create Stage2");
            areaGo.transform.SetParent(rootTransform);
            var area = Undo.AddComponent<StageArea>(areaGo);
            var areaSo = new SerializedObject(area);
            areaSo.FindProperty("areaId").stringValue = "Area1";
            areaSo.FindProperty("contentsRoot").objectReferenceValue = root;
            areaSo.FindProperty("startPoint").objectReferenceValue = spawnAnchor;
            areaSo.ApplyModifiedProperties();

            // エリア・トリガーの操作窓口はStageView。Definitionは初期エリアとViewの在り処だけ持つ
            var stageView = root != null ? root.GetComponent<StageView>() : null;
            WireStageView(stageView, new[] { area }, triggers);

            var definitionSo = new SerializedObject(definition);
            definitionSo.FindProperty("initialArea").objectReferenceValue = area;
            definitionSo.FindProperty("view").objectReferenceValue = stageView;
            definitionSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Stage2Creator] ステージ2を生成し、{ScenePath} に保存しました。");
        }

        static SplineContainer CreateSpline(string name, Vector3[] knots)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage2 Spline");
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

        /// <summary>
        /// Spline上の指定位置に、イベント用のトリガー(透明な箱)を置く。
        /// </summary>
        static AreaTrigger CreateEventTrigger(
            string name, SplineContainer target, float t, Vector3 size, Transform parent)
        {
            Vector3 pos = (Vector3)target.EvaluatePosition(t);
            Vector3 tan = (Vector3)target.EvaluateTangent(t);
            tan.y = 0.0f;
            Quaternion rot = tan.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(tan.normalized)
                : Quaternion.identity;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage2");
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, rot);

            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            box.size = size;
            box.center = new Vector3(0.0f, size.y * 0.5f, 0.0f);

            return Undo.AddComponent<AreaTrigger>(go);
        }

        /// <summary>
        /// AreaTriggerにイベント情報を配線する。
        /// </summary>
        static void WireTriggerInfo(AreaTrigger trigger, AreaEventInfo info)
        {
            var so = new SerializedObject(trigger);
            so.FindProperty("eventInfo").objectReferenceValue = info;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// StageViewへ全エリアと全トリガーを配線する。
        /// </summary>
        static void WireStageView(StageView view, IReadOnlyList<StageArea> areas, IReadOnlyList<AreaTrigger> triggers)
        {
            if (view == null)
            {
                Debug.LogError("[Stage2Creator] StageView not found on GeneratedStage.");
                return;
            }

            var so = new SerializedObject(view);

            var areasProperty = so.FindProperty("areas");
            areasProperty.arraySize = areas.Count;
            for (var i = 0; i < areas.Count; i++)
            {
                areasProperty.GetArrayElementAtIndex(i).objectReferenceValue = areas[i];
            }

            var triggersProperty = so.FindProperty("areaTriggers");
            triggersProperty.arraySize = triggers.Count;
            for (var i = 0; i < triggers.Count; i++)
            {
                triggersProperty.GetArrayElementAtIndex(i).objectReferenceValue = triggers[i];
            }

            so.ApplyModifiedProperties();
        }
    }
}
