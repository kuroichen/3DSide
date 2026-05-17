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
        protected float speed = 5.0f;

        [SerializeField, Range(0, 1)]
        protected float t = 0.0f;

        public event Action<bool> OnSplineDirectionChanged = null;

        public Vector3 Position { get; private set; }
        public Vector3 Tangent { get; private set; }
        public SplineContainer Spline => this.splineContainer;
        public float T => this.t;

        Vector3 previousPosition = Vector3.zero;
        float previousT = 0.0f;


        protected virtual void Awake(){}


        protected virtual void Update()
        {
            // 毎フレーム前のTを記録
            previousT = this.t;

            // 入力を取得
            var input = GetInput();

            // Splineの実際の長さを取得
            float length = this.splineContainer.CalculateLength();

            // tを進める
            this.t += (input * speed / length) * Time.deltaTime;
            this.t = Mathf.Clamp01(t);

            // t値からSpline上の接線(進行方向)を取得
            this.Position = (Vector3)this.splineContainer.EvaluatePosition(t);
            this.Tangent = (Vector3)this.splineContainer.EvaluateTangent(t);

            // ゲート通過判定
            CheckGateCrossing();

            previousPosition = this.transform.position;
        }

        public void RevertT()
        {
            this.t = previousT;
            this.Position = (Vector3)this.splineContainer.EvaluatePosition(this.t);
            this.Tangent = (Vector3)this.splineContainer.EvaluateTangent(this.t);
        }

        public void SwitchSpline(SplineContainer newSpline)
        {
            Debug.Log($"SwitchSpline: {newSpline.name}");

            this.splineContainer = newSpline;

            // 現在位置から最近位置を取得
            SplineUtility.GetNearestPoint(
                this.splineContainer.Spline,
                this.splineContainer.transform.InverseTransformPoint(
                    this.transform.position),
                out _,
                out float nearestT);

            this.t = nearestT;
            this.Position = (Vector3)this.splineContainer.EvaluatePosition(this.t);
            this.Tangent = (Vector3)this.splineContainer.EvaluateTangent(this.t);

            // 接線のX成分でSplineの向きを判定してカメラに通知
            var isRightward = this.Tangent.x >= 0;
            OnSplineDirectionChanged?.Invoke(isRightward);
        }

        protected virtual float GetInput() => 0.0f;

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
