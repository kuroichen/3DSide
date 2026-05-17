using UnityEngine;

namespace Rail
{
    public class SplineFollowCamera : MonoBehaviour
    {
        [SerializeField]
        SplineMover targetMover = null;

        [SerializeField]
        Vector3 offset = Vector3.zero;

        [SerializeField]
        float positionSmooth = 5.0f;

        [SerializeField]
        float rotationSmooth = 5.0f;

        void Start()
        {
            this.targetMover.OnSplineDirectionChanged += isRightward =>
            {
                //this.offset.z = isRightward
                    //? -Mathf.Abs(this.offset.z) // 右向き → 左側から映す
                    //: Mathf.Abs(this.offset.z); // 左向き → 右側から映す
            };
        }

        void LateUpdate()
        {
            // 接線の方向を取得
            var tangent = this.targetMover.Spline.EvaluateTangent(this.targetMover.T);

            // 接線に対して垂直な方向を取得する
            // 道が曲がっていても「常に横から見る」を維持する
            var right = Vector3.Cross(Vector3.up, tangent).normalized;

            // カメラの目標位置を計算
            var targetPosition = this.targetMover.transform.position 
                + right * offset.z
                + Vector3.up * offset.y;

            // 位置を補間
            this.transform.position = Vector3.Lerp(
                this.transform.position,
                targetPosition,
                this.positionSmooth * Time.deltaTime);
            
            // 対象を見る
            this.transform.LookAt(this.targetMover.transform.position);
        }
    }
}
