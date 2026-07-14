using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// Splineから道ジオメトリ(道メッシュ・キューブ敷き)を生成する共通ユーティリティ。
    /// RailStageToolWindowとStageAutoBuilderWindowの両方から利用する。
    /// 各メソッドは生成したGameObjectを返し、任意で親Transform配下にまとめられる。
    /// </summary>
    public static class StageGeometryUtility
    {
        /// <summary>
        /// Splineに沿って平らな道メッシュ(MeshColliderつき)を生成する。
        /// </summary>
        public static GameObject GenerateRoadMesh(
            SplineContainer container, float width, int samples, Transform parent)
        {
            int n = Mathf.Max(1, samples);
            float hw = width * 0.5f;

            var verts = new Vector3[(n + 1) * 2];
            var uvs = new Vector2[(n + 1) * 2];
            var tris = new int[n * 6];

            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                Vector3 pos = container.EvaluatePosition(t);
                Vector3 tan = ((Vector3)container.EvaluateTangent(t));
                if (tan.sqrMagnitude < 0.0001f) tan = Vector3.right;
                tan.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, tan).normalized;

                verts[i * 2] = pos - right * hw;
                verts[i * 2 + 1] = pos + right * hw;
                uvs[i * 2] = new Vector2(0.0f, t * n);
                uvs[i * 2 + 1] = new Vector2(1.0f, t * n);
            }

            for (int i = 0; i < n; i++)
            {
                int v = i * 2;
                int ti = i * 6;
                tris[ti] = v;
                tris[ti + 1] = v + 2;
                tris[ti + 2] = v + 1;
                tris[ti + 3] = v + 1;
                tris[ti + 4] = v + 2;
                tris[ti + 5] = v + 3;
            }

            var mesh = new Mesh { name = "RoadMesh" };
            mesh.indexFormat = verts.Length > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject($"RoadMesh_{container.name}");
            Undo.RegisterCreatedObjectUndo(go, "Generate Road Mesh");
            go.transform.position = Vector3.zero;
            if (parent != null) go.transform.SetParent(parent, true);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateDefaultMaterial();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            return go;
        }

        /// <summary>
        /// Splineに沿ってキューブ(BoxColliderつき)を並べる。ブロックアウト向け。
        /// holeRangesを渡すと、そのレール距離(railZ)範囲には道を敷かない(穴あき道・飛び石用)。
        /// </summary>
        public static GameObject LayCubes(
            SplineContainer container, float spacing, float width, float thickness,
            bool tiltToSlope, Transform parent, IReadOnlyList<Vector2> holeRanges = null,
            Material material = null)
        {
            float length = container.CalculateLength();
            float sp = Mathf.Max(0.1f, spacing);
            int count = Mathf.Max(1, Mathf.CeilToInt(length / sp));

            var root = new GameObject($"CubeRoad_{container.name}");
            Undo.RegisterCreatedObjectUndo(root, "Lay Cubes");
            if (parent != null) root.transform.SetParent(parent, true);

            for (int i = 0; i <= count; i++)
            {
                float t = (float)i / count;

                // 穴の範囲は敷かない
                if (IsInHole(t * length, holeRanges)) continue;
                Vector3 pos = container.EvaluatePosition(t);
                Vector3 tan = ((Vector3)container.EvaluateTangent(t));
                if (tan.sqrMagnitude < 0.0001f) tan = Vector3.right;
                tan.Normalize();

                Vector3 forward = tiltToSlope ? tan : new Vector3(tan.x, 0.0f, tan.z).normalized;
                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.right;

                // 端のキューブはレール端から食み出さないよう内側に寄せる
                // (食み出すと、レール端の余白から歩いて降りる時に足場が残ってしまい落下できない)
                float cubeLength = sp * 1.1f;
                if (i == 0) pos += forward * (cubeLength * 0.5f);
                if (i == count) pos -= forward * (cubeLength * 0.5f);

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(cube, "Lay Cubes");
                if (material != null)
                {
                    cube.GetComponent<MeshRenderer>().sharedMaterial = material;
                }
                cube.transform.SetParent(root.transform);
                cube.transform.position = pos - Vector3.up * (thickness * 0.5f);
                cube.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                // x=道幅, y=厚み, z=間隔より少し長めにして隙間防止
                cube.transform.localScale = new Vector3(width, thickness, sp * 1.1f);
            }

            return root;
        }

        static bool IsInHole(float railZ, IReadOnlyList<Vector2> holeRanges)
        {
            if (holeRanges == null) return false;

            foreach (var range in holeRanges)
            {
                if (railZ >= range.x && railZ <= range.y) return true;
            }
            return false;
        }

        /// <summary>
        /// URP/Standardのデフォルトマテリアルを生成する。
        /// </summary>
        public static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            return new Material(shader);
        }
    }
}
