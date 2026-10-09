using UnityEngine;

namespace Rail
{
    public class HitPointUI : MonoBehaviour
    {
        const int MaxHitPoint = 3;

        [SerializeField]
        int hitPoint = MaxHitPoint;

        [SerializeField]
        GameObject[] hitPointObjects = null;

        public void SetHitPoint(int hitPoint)
        {
            hitPoint = Mathf.Clamp(hitPoint, 0, MaxHitPoint);

            for (int i = 0; i < this.hitPointObjects.Length; i++)
            {
                this.hitPointObjects[i].SetActive(i < hitPoint);
            }
        }

        private void OnValidate()
        {
            SetHitPoint(this.hitPoint);
        }
    }
}
