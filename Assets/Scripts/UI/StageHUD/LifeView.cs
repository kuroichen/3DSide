using UnityEngine;

namespace Rail
{
    public sealed class LifeView : MonoBehaviour
    {
        [SerializeField]
        GameObject[] lifePoints = null;

        public void SetPoint(int hp)
        {
            var length = this.lifePoints.Length;
            for (var i = 0; i < length; i++)
            {
                var active = i < hp;
                var lifePoint = this.lifePoints[i];

                if (lifePoint.activeSelf != active)
                {
                    lifePoint.SetActive(active);
                }
            }
        }
    }
}
