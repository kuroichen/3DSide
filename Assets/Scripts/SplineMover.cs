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

        public Vector3 Position { get; private set; }
        public Vector3 Tangent { get; private set; }
        public SplineContainer Spline => this.splineContainer;
        public float T => this.t;

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
        }

        public void RevertT()
        {
            this.t = previousT;
            this.Position = (Vector3)this.splineContainer.EvaluatePosition(this.t);
            this.Tangent = (Vector3)this.splineContainer.EvaluateTangent(this.t);
        }

        protected virtual float GetInput() => 0.0f;
    }
}
