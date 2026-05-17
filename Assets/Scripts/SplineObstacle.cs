using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public class SplineObstacle : MonoBehaviour
    {
        [SerializeField]
        SplineContainer spline = null;

        [SerializeField, Range(0.0f, 1.0f)]
        float t = 0.1f;

        void OnValidate()
        {
            if (this.spline == null) return;

            var position = (Vector3)this.spline.EvaluatePosition(t);
            var tangent = (Vector3)this.spline.EvaluateTangent(t);

            // 位置と向きを自動設定
            this.transform.position = position;

            // 接線方向に向けることで自動的に垂直になる
            if (tangent != Vector3.zero)
            {
                this.transform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }
}
