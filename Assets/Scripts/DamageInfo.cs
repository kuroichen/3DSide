using UnityEngine;


namespace Rail
{
    public struct DamageInfo
    {
        public int damage;

        // ヒットした位置
        public Vector3 hitPoint;

        // 攻撃者
        public Transform attacker;
    }
}
