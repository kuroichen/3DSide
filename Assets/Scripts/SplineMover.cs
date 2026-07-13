using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    /// <summary>
    /// Spline移動を行うためのコンポーネント
    /// ステージ上に配置されるすべてのオブジェクトはこれを使用する想定
    /// </summary>
    public class SplineMover : MonoBehaviour
    {
        [SerializeField]
        protected SplineContainer splineContainer = null;
        
        [SerializeField]
        protected Animator animator = null;

        [SerializeField]
        protected float speed = 5.0f;

        [SerializeField, Range(0, 1)]
        protected float t = 0.0f;

        [SerializeField]
        float impulseDecay = 10.0f; // 外力の減衰速度

        public event Action<bool> OnSplineDirectionChanged = null;

        public Vector3 Position { get; private set; }
        public Vector3 Tangent { get; private set; }

        public SplineContainer Spline => this.splineContainer;
        public SplineRail Rail => this.currentRail;
        public float T => this.t;
        public float RailZ => this.railZ;

        // めり込み防止
        Vector3 previousPosition = Vector3.zero;
        float previousRailZ = 0.0f;

        // 外力
        protected float impulseVelocityT = 0.0f;
        protected SplineRail currentRail = null;
        protected float railZ = 0.0f;

        // 子クラスoverride用
        protected virtual float Speed => this.speed;

        // 入力の効き(着地硬直などで一時的に入力を殺すために子クラスが上書きする)
        protected virtual float InputScale => 1.0f;

        protected virtual void Awake()
        {
            ResolveRail();
            SetRailZFromT(this.t);
            EvaluateRailPosition();
        }


        protected virtual void Update()
        {
            if (this.currentRail == null)
            {
                ResolveRail();
                if (this.currentRail == null) return;
            }

            this.previousRailZ = this.railZ;

            // 入力を取得
            var input = GetInput() * InputScale;

            // 通常入力 + 外力を合算してレール距離を進める
            var totalInput = input + this.impulseVelocityT;
            this.railZ += totalInput * this.Speed * Time.deltaTime;

            // 端の外へ余白の分だけはみ出せる(浮きレールから歩いて降りる→落下中に下のレールへ乗り換える)
            this.railZ = Mathf.Clamp(
                this.railZ,
                -this.currentRail.EndMargin,
                this.currentRail.Length + this.currentRail.EndMargin);
            this.t = this.currentRail.RailZToT(this.railZ);

            // 外力を減衰
            this.impulseVelocityT = Mathf.Lerp(this.impulseVelocityT, 0.0f, this.impulseDecay * Time.deltaTime);

            EvaluateRailPosition();

            TrySwitchNearestRail(input);

            // ゲート通過判定
            CheckGateCrossing();

            this.previousPosition = this.transform.position;

            if (this.animator != null)
            {
                this.animator.SetFloat("Speed", Mathf.Abs(input));
            }
        }

        public void RevertT()
        {
            this.railZ = this.previousRailZ;
            this.t = this.currentRail.RailZToT(this.railZ);
            EvaluateRailPosition();
        }

        public virtual void SetSpline(SplineContainer newSpline, float newT)
        {
            this.splineContainer = newSpline;
            ResolveRail();
            this.t = Mathf.Clamp01(newT);
            SetRailZFromT(this.t);
            EvaluateRailPosition();

            NotifySplineDirectionChanged();
        }

        public void SwitchSpline(SplineContainer newSpline)
        {
            Debug.Log($"SwitchSpline: {newSpline.name}");

            this.splineContainer = newSpline;
            ResolveRail();

            var hit = this.currentRail != null
                ? this.currentRail.World2Local(this.transform.position)
                : null;

            if (hit.HasValue)
            {
                this.railZ = Mathf.Clamp(
                    hit.Value.z,
                    -this.currentRail.EndMargin,
                    this.currentRail.Length + this.currentRail.EndMargin);
                this.t = this.currentRail.RailZToT(this.railZ);
            }
            else
            {
                SetRailZFromT(this.t);
            }

            EvaluateRailPosition();
            NotifySplineDirectionChanged();
        }

        protected virtual float GetInput() => 0.0f;

        void ResolveRail()
        {
            if (this.splineContainer == null) return;

            this.currentRail = this.splineContainer.GetComponent<SplineRail>();
            if (this.currentRail == null)
            {
                this.currentRail = this.splineContainer.gameObject.AddComponent<SplineRail>();
            }

            RailManager.Instance.Add(this.currentRail);
        }

        void SetRailZFromT(float newT)
        {
            if (this.currentRail == null) return;

            this.t = Mathf.Clamp01(newT);
            this.railZ = this.currentRail.TToRailZ(this.t);
        }

        void EvaluateRailPosition()
        {
            if (this.currentRail == null) return;

            this.Position = this.currentRail.Local2World(new Vector3(0.0f, 0.0f, this.railZ));
            var tangent = this.currentRail.Rotation(this.railZ) * Vector3.forward;
            tangent.y = 0.0f;
            this.Tangent = tangent.sqrMagnitude > 0.0001f
                ? tangent.normalized
                : Vector3.forward;
        }

        void TrySwitchNearestRail(float input)
        {
            var hit = RailManager.Instance.World2Local(this.transform.position, this.currentRail, input);
            if (!hit.HasValue || hit.Value.Rail == this.currentRail) return;

            this.currentRail = hit.Value.Rail;
            this.splineContainer = this.currentRail.SplineContainer;

            // 余白の分は端の外も受け付ける(切替時の吸い付き=ワープを防ぐ)
            this.railZ = Mathf.Clamp(
                hit.Value.LocalPosition.z,
                -this.currentRail.EndMargin,
                this.currentRail.Length + this.currentRail.EndMargin);
            this.t = this.currentRail.RailZToT(this.railZ);
            EvaluateRailPosition();
            NotifySplineDirectionChanged();
        }

        void NotifySplineDirectionChanged()
        {
            var isRightward = this.Tangent.x >= 0;
            OnSplineDirectionChanged?.Invoke(isRightward);
        }

        void CheckGateCrossing()
        {
            var moveVector = this.transform.position - this.previousPosition;

            if (moveVector.magnitude < 0.001f) return;

            if (Physics.Raycast(
                origin: this.previousPosition,
                direction: moveVector.normalized,
                out RaycastHit hit,
                maxDistance: moveVector.magnitude,
                layerMask: LayerMask.GetMask("Gate")))
            {
                var gate = hit.collider.GetComponent<SplineGate>();
                if (gate == null) return;

                // 通過方向を判定
                // 移動方向と法線の内積が負→表から裏へ
                var fromFront = Vector3.Dot(moveVector.normalized, gate.transform.forward) < 0;
                gate.OnCrossed(this, fromFront);
            }
        }
    }
}
