using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// デモ用のSplineを引いて、そのままStage Auto Builderでステージを生成するメニュー。
    /// ツールの動作確認用。
    /// Library/create_demo_stage_request が存在する場合はコンパイル直後に一度だけ自動実行する(外部ツール連携用)。
    /// rev.4
    /// </summary>
    public static class DemoStageCreator
    {
        const string RequestMarkerPath = "Library/create_demo_stage_request";

        [MenuItem("Tools/Rail/Create Demo Stage")]
        public static void CreateDemoStage()
        {
            // 直線 → 上り坂 → 右カーブ → 下り坂 のデモコース
            var knots = new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(12.0f, 0.0f, 0.0f),
                new Vector3(20.0f, 2.5f, 0.0f),
                new Vector3(28.0f, 2.5f, 4.0f),
                new Vector3(32.0f, 2.5f, 12.0f),
                new Vector3(32.0f, 0.0f, 22.0f),
                new Vector3(32.0f, 0.0f, 32.0f),
            };

            var go = new GameObject("DemoStageSpline");
            Undo.RegisterCreatedObjectUndo(go, "Create Demo Stage Spline");
            go.transform.position = Vector3.zero;

            var container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var knot in knots)
            {
                spline.Add(new BezierKnot((float3)knot), TangentMode.AutoSmooth);
            }

            Debug.Log("[DemoStageCreator] デモSplineを生成しました。Stage Auto Builderを実行します。");

            // Stage Auto Builderを開いて即ビルド(設定はウィンドウの現在値)
            var window = EditorWindow.GetWindow<StageAutoBuilderWindow>("Stage Auto Builder");
            window.Build();
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            Debug.Log("[DemoStageCreator] 実行要求マーカーを検出しました。次のエディタ更新で実行します。");

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
                CreateDemoStage();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
