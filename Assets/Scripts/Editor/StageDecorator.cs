using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// StageThemeに従ってSpline沿いに飾り(草・岩・木など)を散布し、環境設定を適用する。
    /// 乱数はテーマのシードで固定するため、再生成しても同じ配置になる。
    /// </summary>
    public static class StageDecorator
    {
        /// <summary>
        /// Spline沿いの道の縁に飾りを散布する。
        /// 足元に地面(コライダー)が無い場所には置かないため、道の生成後に呼ぶこと。
        /// </summary>
        public static GameObject Scatter(SplineContainer container, StageTheme theme, Transform parent)
        {
            var root = new GameObject($"Decoration_{container.name}");
            Undo.RegisterCreatedObjectUndo(root, "Scatter Decoration");
            if (parent != null) root.transform.SetParent(parent, true);

            var random = new System.Random(theme.seed ^ container.name.GetHashCode());
            var length = container.CalculateLength();

            foreach (var entry in theme.scatter)
            {
                if (entry.prefabs == null || entry.prefabs.Length == 0) continue;

                var count = Mathf.RoundToInt(entry.densityPer10m * length / 10.0f);
                for (var i = 0; i < count; i++)
                {
                    PlaceOne(container, entry, random, root.transform);
                }
            }

            return root;
        }

        static void PlaceOne(
            SplineContainer container,
            StageTheme.ScatterEntry entry,
            System.Random random,
            Transform parent)
        {
            var t = (float)random.NextDouble();
            var center = (Vector3)container.EvaluatePosition(t);
            var tangent = ((Vector3)container.EvaluateTangent(t));
            tangent.y = 0.0f;
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
            tangent.Normalize();

            var right = Vector3.Cross(Vector3.up, tangent).normalized;

            // 置く側を決める(FarSideはカメラの反対側=+z側)
            float sideSign;
            if (entry.side == StageTheme.ScatterSide.FarSide)
            {
                sideSign = right.z >= 0.0f ? 1.0f : -1.0f;
            }
            else
            {
                sideSign = random.Next(2) == 0 ? 1.0f : -1.0f;
            }

            var offset = Mathf.Lerp(entry.minSideOffset, entry.maxSideOffset, (float)random.NextDouble());
            var position = center + right * (offset * sideSign);

            // 足元に道が無い場所(穴・ギャップの脇)には置かない
            var rayOrigin = position + Vector3.up * 2.0f;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, 30.0f))
            {
                return;
            }
            position.y = hit.point.y;

            var prefab = entry.prefabs[random.Next(entry.prefabs.Length)];
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;

            Undo.RegisterCreatedObjectUndo(instance, "Scatter Decoration");
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0.0f, (float)random.NextDouble() * 360.0f, 0.0f);
            instance.transform.localScale = Vector3.one
                * Mathf.Lerp(entry.minScale, entry.maxScale, (float)random.NextDouble());
        }

        /// <summary>
        /// テーマの環境設定(空・フォグ・太陽光)をアクティブシーンへ適用する。
        /// </summary>
        public static void ApplyEnvironment(StageTheme theme)
        {
            if (theme.skyboxMaterial != null)
            {
                RenderSettings.skybox = theme.skyboxMaterial;
            }

            RenderSettings.fog = theme.enableFog;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;

            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }

            if (sun != null)
            {
                sun.color = theme.sunColor;
                sun.transform.rotation = Quaternion.Euler(theme.sunEuler);
                RenderSettings.sun = sun;
            }
        }
    }
}
