using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    [RequireComponent(typeof(SplineContainer))]
    public class SplineRail : MonoBehaviour, IRail
    {
        [SerializeField]
        SplineContainer splineContainer = null;

        [SerializeField]
        float maxHorizontalOffset = 2.0f;

        [SerializeField]
        float maxVerticalOffset = 5.0f;

        [SerializeField]
        float endMargin = 1.0f; // レール端の外へはみ出せる余白(浮きレールから歩いて降りるため)

        public SplineContainer SplineContainer => this.splineContainer;
        public float Length => Mathf.Max(0.001f, this.splineContainer.CalculateLength());
        public float EndMargin => this.endMargin;

        void Reset()
        {
            this.splineContainer = GetComponent<SplineContainer>();
        }

        void Awake()
        {
            if (this.splineContainer == null)
            {
                this.splineContainer = GetComponent<SplineContainer>();
            }
        }

        void OnEnable()
        {
            RailManager.Instance.Add(this);
        }

        void OnDisable()
        {
            if (RailManager.HasInstance)
            {
                RailManager.Instance.Remove(this);
            }
        }

        public Vector3 Local2World(Vector3 localPosition)
        {
            var clampedZ = Mathf.Clamp(localPosition.z, 0.0f, Length);
            var t = RailZToT(clampedZ);
            var center = (Vector3)this.splineContainer.EvaluatePosition(t);
            var rotation = Rotation(clampedZ);

            // 端の外(余白)は端の接線方向へ直線外挿する
            var overshoot = localPosition.z - clampedZ;
            if (overshoot != 0.0f)
            {
                center += rotation * Vector3.forward * overshoot;
            }

            return center
                + rotation * Vector3.right * localPosition.x
                + rotation * Vector3.up * localPosition.y;
        }

        public Vector3? World2Local(Vector3 worldPosition)
        {
            SplineUtility.GetNearestPoint(
                this.splineContainer.Spline,
                this.splineContainer.transform.InverseTransformPoint(worldPosition),
                out float3 nearest,
                out float t);

            var center = this.splineContainer.transform.TransformPoint((Vector3)nearest);
            var railZ = TToRailZ(t);
            var rotation = Rotation(railZ);
            var delta = worldPosition - center;

            // 端にクランプされている場合、接線方向のはみ出し量をzに反映する。
            // 余白を超えて端の外にいる相手はこのレールの担当外(でないと、
            // 端の延長線上にいるだけで「差は高さだけ」と誤判定されて吸い付く=ワープする)
            if (t <= 0.0001f || t >= 0.9999f)
            {
                var overshoot = Vector3.Dot(delta, rotation * Vector3.forward);
                if (Mathf.Abs(overshoot) > this.endMargin) return null;

                railZ += overshoot;
                center += rotation * Vector3.forward * overshoot;
                delta = worldPosition - center;
            }

            var local = new Vector3(
                Vector3.Dot(delta, rotation * Vector3.right),
                Vector3.Dot(delta, rotation * Vector3.up),
                railZ);

            if (Mathf.Abs(local.x) > this.maxHorizontalOffset
                || Mathf.Abs(local.y) > this.maxVerticalOffset)
            {
                return null;
            }

            return local;
        }

        public Quaternion Rotation(float railZ)
        {
            var t = RailZToT(railZ);
            var tangent = (Vector3)this.splineContainer.EvaluateTangent(t);
            tangent.y = 0.0f;

            if (tangent.sqrMagnitude <= 0.0001f)
            {
                return this.transform.rotation;
            }

            return Quaternion.LookRotation(tangent.normalized, Vector3.up);
        }

        public float TToRailZ(float t)
        {
            return Mathf.Clamp01(t) * Length;
        }

        public float RailZToT(float railZ)
        {
            return Mathf.Clamp01(railZ / Length);
        }
    }
}
