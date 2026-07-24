using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public class SplineGate : MonoBehaviour
    {
        [SerializeField]
        SplineContainer splineOnFront = null;

        [SerializeField]
        SplineContainer splineOnBack = null;

        public void OnCrossed(SplineMover mover, bool fromFront)
        {
            var target = fromFront
                ? splineOnBack
                : splineOnFront;
            
            if (target != null)
            {
                mover.SwitchSpline(target);
            }
        }
    }
}
