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

        // 最近傍点検索の精度。デフォルト(分割4)だと曲がった長いレールで
        // 数mズレたrailZを返すことがあり、乗り換え時のワープの原因になる
        const int NearestPointResolution = 16;
        const int NearestPointIterations = 8;

        float cachedLength = -1.0f;

        public SplineContainer SplineContainer => this.splineContainer;
        public float EndMargin => this.endMargin;

        public float Length
        {
            get
            {
                // CalculateLengthはスプライン全体を歩くため毎アクセス計算はしない
                if (this.cachedLength < 0.0f)
                {
                    this.cachedLength = Mathf.Max(0.001f, this.splineContainer.CalculateLength());
                }
                return this.cachedLength;
            }
        }

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
            Spline.Changed += OnSplineChanged;

            // 無効化中のSpline編集はChangedイベントで拾えないため、有効化時に取り直す
            this.cachedLength = -1.0f;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;

            if (RailManager.HasInstance)
            {
                RailManager.Instance.Remove(this);
            }
        }

        void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (this.splineContainer != null && spline == this.splineContainer.Spline)
            {
                this.cachedLength = -1.0f;
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
                out float t,
                NearestPointResolution,
                NearestPointIterations);

            // 3D最近傍を初期値に、水平距離(XZ)最小のrailZへ絞り込む。
            // このレール系はx/zをレール・yを物理が受け持つ設計なので、レール座標も
            // 水平位置で決める。3D距離のままだと、坂の上空にいる時に斜め後ろ上の
            // 点へ吸い付き、落下中に後方へ引き戻される
            var railZ = RefineHorizontalNearest(worldPosition, TToRailZ(t));
            t = RailZToT(railZ);

            var center = (Vector3)this.splineContainer.EvaluatePosition(t);
            var rotation = Rotation(railZ);
            var delta = worldPosition - center;

            // 端にクランプされている場合、接線方向のはみ出し量をzに反映する。
            // 余白を超えて端の外にいる相手はこのレールの担当外(でないと、
            // 端の延長線上にいるだけで「差は高さだけ」と誤判定されて吸い付く=ワープする)。
            // 判定はt比率でなく距離基準(短いレールでもmm精度で端を検出できるように)
            if (railZ <= 0.001f || railZ >= Length - 0.001f)
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

        /// <summary>
        /// 初期値の近傍で、水平距離が最小になるrailZを三分探索で求める。
        /// </summary>
        float RefineHorizontalNearest(Vector3 worldPosition, float initialRailZ)
        {
            const float SearchRange = 3.0f;
            const int SearchIterations = 20;

            var low = Mathf.Max(0.0f, initialRailZ - SearchRange);
            var high = Mathf.Min(Length, initialRailZ + SearchRange);

            for (var i = 0; i < SearchIterations; i++)
            {
                var mid1 = Mathf.Lerp(low, high, 1.0f / 3.0f);
                var mid2 = Mathf.Lerp(low, high, 2.0f / 3.0f);

                if (HorizontalSqrDistance(worldPosition, mid1) < HorizontalSqrDistance(worldPosition, mid2))
                {
                    high = mid2;
                }
                else
                {
                    low = mid1;
                }
            }

            return (low + high) * 0.5f;
        }

        float HorizontalSqrDistance(Vector3 worldPosition, float railZ)
        {
            var point = (Vector3)this.splineContainer.EvaluatePosition(RailZToT(railZ));
            var dx = worldPosition.x - point.x;
            var dz = worldPosition.z - point.z;
            return dx * dx + dz * dz;
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
            // スプラインのtは弧長に比例しない(ノット間隔・曲率で伸縮する)ため、
            // 比例計算ではなく距離単位へ正しく変換する。カーブ区間の位置ズレ・速度ムラ対策
            return this.splineContainer.Spline.ConvertIndexUnit(
                Mathf.Clamp01(t), PathIndexUnit.Normalized, PathIndexUnit.Distance);
        }

        public float RailZToT(float railZ)
        {
            var clamped = Mathf.Clamp(railZ, 0.0f, Length);
            return this.splineContainer.Spline.ConvertIndexUnit(
                clamped, PathIndexUnit.Distance, PathIndexUnit.Normalized);
        }
    }
}
