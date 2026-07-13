using UnityEngine;
using UnityEngine.Splines;


namespace Rail
{
    public enum SplineAnchorDirection 
    {
        Forward,    // 接線(進行)方向
        Backward,   
        Right,      // 接線(進行)方向に対して右
        Left
    }

    public class SplineAnchor : MonoBehaviour
    {
        [SerializeField]
        protected SplineContainer splineContainer = null;

        [SerializeField]
        SplineAnchorDirection direction = SplineAnchorDirection.Forward;

        [SerializeField, Range(0.0f, 1.0f)]
        float t = 0.0f;

        public SplineContainer SplineContainer => this.splineContainer;
        public float T => this.t;

        public Quaternion GetRotation()
        {
            var tangent = (Vector3)this.splineContainer.EvaluateTangent(this.t);
            tangent.y = 0.0f;
            tangent.Normalize();

            var forward = this.direction switch
            {
                SplineAnchorDirection.Forward => tangent,
                SplineAnchorDirection.Backward => -tangent,
                SplineAnchorDirection.Right => Vector3.Cross(Vector3.up, tangent) * -1,
                SplineAnchorDirection.Left => Vector3.Cross(Vector3.up, tangent),
                _ => tangent  
            };

            return Quaternion.LookRotation(forward);
        }

        Vector3 GetPosition()
        {
            return (Vector3)this.splineContainer.EvaluatePosition(this.t);
        }

        void OnValidate()
        {
            if (this.splineContainer == null) return;
            this.t = Mathf.Clamp01(this.t);
            this.transform.position = GetPosition();
            this.transform.rotation = GetRotation();
        }
    }
}
