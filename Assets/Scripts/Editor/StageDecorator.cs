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
        /// 遠景(近景の丘・遠景の山・谷底の暗い床)を生成する。
        /// 岩モデルを巨大化しフォグに溶かして山のシルエットにする。
        /// </summary>
        public static GameObject GenerateHorizon(SplineContainer container, StageTheme theme, Transform parent)
        {
            var root = new GameObject($"Horizon_{container.name}");
            Undo.RegisterCreatedObjectUndo(root, "Generate Horizon");
            if (parent != null) root.transform.SetParent(parent, true);

            if (theme.horizonPrefabs == null || theme.horizonPrefabs.Length == 0) return root;

            var random = new System.Random(theme.seed ^ 0x5A5A);
            var length = container.CalculateLength();

            // ステージの端の外まで並べて「世界の続き」を見せる
            for (var distance = -30.0f; distance <= length + 30.0f; distance += 12.0f)
            {
                var t = Mathf.Clamp01(distance / length);
                var center = (Vector3)container.EvaluatePosition(t);
                var tangent = ((Vector3)container.EvaluateTangent(t));
                tangent.y = 0.0f;
                if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.right;
                tangent.Normalize();

                var right = Vector3.Cross(Vector3.up, tangent).normalized;
                // 奥側(カメラの反対=+z側)に置く
                var farSign = right.z >= 0.0f ? 1.0f : -1.0f;

                // 近景の丘(緑)
                PlaceSilhouette(theme, random, root.transform, center,
                    right * (farSign * (22.0f + (float)random.NextDouble() * 12.0f)),
                    6.0f + (float)random.NextDouble() * 6.0f,
                    theme.nearHillMaterial);

                // 遠景の山(青灰)は1つおきに
                if (random.Next(2) == 0)
                {
                    PlaceSilhouette(theme, random, root.transform, center,
                        right * (farSign * (45.0f + (float)random.NextDouble() * 40.0f)),
                        16.0f + (float)random.NextDouble() * 16.0f,
                        theme.farMountainMaterial);
                }
            }

            // 谷底の暗い床(視覚のみ。コライダー無し=落下死は維持される)
            if (theme.abyssMaterial != null)
            {
                var abyss = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(abyss, "Generate Horizon");
                Object.DestroyImmediate(abyss.GetComponent<Collider>());
                abyss.name = "AbyssFloor";
                abyss.transform.SetParent(root.transform);
                abyss.transform.position = new Vector3(length * 0.5f, -12.0f, 20.0f);
                abyss.transform.localScale = new Vector3(length + 120.0f, 0.5f, 200.0f);
                abyss.GetComponent<MeshRenderer>().sharedMaterial = theme.abyssMaterial;
            }

            return root;
        }

        static void PlaceSilhouette(
            StageTheme theme, System.Random random, Transform parent,
            Vector3 center, Vector3 offset, float scale, Material material)
        {
            var prefab = theme.horizonPrefabs[random.Next(theme.horizonPrefabs.Length)];
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;

            Undo.RegisterCreatedObjectUndo(instance, "Generate Horizon");
            instance.transform.SetParent(parent);
            var position = center + offset;
            position.y = center.y - 2.0f; // 少し沈めて根元を隠す
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0.0f, (float)random.NextDouble() * 360.0f, 0.0f);
            instance.transform.localScale = Vector3.one * scale;

            // シルエット用の単色マテリアルに差し替え、コライダーは外す
            if (material != null)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var materials = new Material[renderer.sharedMaterials.Length];
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                }
            }
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }
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

            // アンビエントを三色制御にして色被り(青被り・黄被り)を抑える
            // 空=青白 / 水平=中間 / 地面の照り返し=暖色
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.33f, 0.27f);

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
