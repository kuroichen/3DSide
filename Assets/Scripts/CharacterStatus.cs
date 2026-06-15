using UnityEngine;

namespace Rail
{
    [CreateAssetMenu(fileName = "CharacterStatus", menuName = "Rail/CharacterStatus")]
    public class CharacterStatus : ScriptableObject
    {
        [Header("基本ステータス")]
        public int hp = 3;
        public float speed = 5.0f;
        public float jumpForce = 8.0f;

        [Header("攻撃")]
        public int attack = 1;
    }
}
