
using UnityEngine;

namespace Rail
{
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField]
        EnemyController prefab = null;

        [SerializeField]
        SplineAnchor anchor = null;

        public SplineAnchor Anchor => this.anchor;
        public EnemyController Prefab => this.prefab;

        void OnDrawGizmos()
        {
            // シーン上で出現位置を確認できるようにする
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(this.transform.position + Vector3.up, new Vector3(1.0f, 2.0f, 1.0f));
        }
    }
}