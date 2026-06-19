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

        public SplineContainer SplineContainer => this.splineContainer;

        public Quaternion GetRotation()
        {
            var t = GetNearestT();
            var tangent = (Vector3)this.splineContainer.EvaluateTangent(t);
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

        float GetNearestT()
        {
            SplineUtility.GetNearestPoint(
                this.splineContainer.Spline,
                this.splineContainer.transform.InverseTransformPoint(this.transform.position),
                out _,
                out float t);
            
            return t;
        }

        void OnValidate()
        {
            if (this.splineContainer == null) return;
            this.transform.rotation = GetRotation();
        }
    }
}
