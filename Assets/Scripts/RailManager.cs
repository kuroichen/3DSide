using System.Collections.Generic;
using UnityEngine;

namespace Rail
{
    public readonly struct RailHit
    {
        public RailHit(SplineRail rail, Vector3 localPosition)
        {
            Rail = rail;
            LocalPosition = localPosition;
        }

        public SplineRail Rail { get; }
        public Vector3 LocalPosition { get; }
    }

    public class RailManager : MonoBehaviour
    {
        const float TieDistanceEpsilon = 0.0001f;
        const float EndpointTolerance = 0.25f;
        const float AboveTolerance = 0.1f;

        static RailManager instance = null;
        readonly List<SplineRail> rails = new();

        public static bool HasInstance => instance != null;

        public static RailManager Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = FindAnyObjectByType<RailManager>();
                if (instance != null) return instance;

                var go = new GameObject("RailManager");
                instance = go.AddComponent<RailManager>();
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public void Add(SplineRail rail)
        {
            if (rail == null || this.rails.Contains(rail)) return;
            this.rails.Add(rail);
        }

        public void Remove(SplineRail rail)
        {
            this.rails.Remove(rail);
        }

        public RailHit? World2Local(Vector3 worldPosition)
        {
            return World2Local(worldPosition, null, 0.0f);
        }

        public RailHit? World2Local(Vector3 worldPosition, SplineRail currentRail, float input)
        {
            RailHit? bestHit = null;
            var bestDistance = float.MaxValue;
            var bestIsAbove = false;

            foreach (var rail in this.rails)
            {
                if (rail == null || !rail.isActiveAndEnabled) continue;

                var local = rail.World2Local(worldPosition);
                if (!local.HasValue) continue;

                // 「自分がレールと同じ高さ〜上にいる」候補を、「下にいる」候補より常に優先する
                // (浮きレールの下をくぐる・浮きレールから降りる動きが距離比較に負けないように)
                var isAbove = local.Value.y >= -AboveTolerance;
                if (bestHit.HasValue && bestIsAbove && !isAbove) continue;

                var offsetDistance = new Vector2(local.Value.x, local.Value.y).sqrMagnitude;
                if (bestHit.HasValue && bestIsAbove == isAbove)
                {
                    if (offsetDistance > bestDistance + TieDistanceEpsilon) continue;
                    if (Mathf.Abs(offsetDistance - bestDistance) <= TieDistanceEpsilon
                        && ShouldKeepCurrentRail(bestHit, rail, local.Value, currentRail, input))
                    {
                        continue;
                    }
                }

                bestDistance = offsetDistance;
                bestIsAbove = isAbove;
                bestHit = new RailHit(rail, local.Value);
            }

            return bestHit;
        }

        bool ShouldKeepCurrentRail(
            RailHit? bestHit,
            SplineRail candidateRail,
            Vector3 candidateLocal,
            SplineRail currentRail,
            float input)
        {
            if (!bestHit.HasValue) return false;
            if (currentRail == null || candidateRail == currentRail) return true;
            if (bestHit.Value.Rail != currentRail) return true;
            if (Mathf.Abs(input) <= 0.001f) return true;

            var currentLocal = bestHit.Value.LocalPosition;
            var currentAtForwardEnd = currentLocal.z >= currentRail.Length - EndpointTolerance;
            var currentAtBackwardEnd = currentLocal.z <= EndpointTolerance;
            var candidateAtForwardStart = candidateLocal.z <= EndpointTolerance;
            var candidateAtBackwardStart = candidateLocal.z >= candidateRail.Length - EndpointTolerance;

            if (input > 0.0f && currentAtForwardEnd && candidateAtForwardStart)
            {
                return false;
            }

            if (input < 0.0f && currentAtBackwardEnd && candidateAtBackwardStart)
            {
                return false;
            }

            return true;
        }
    }
}
