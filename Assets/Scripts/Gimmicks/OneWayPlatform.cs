using UnityEngine;

namespace Rail
{
    /// <summary>
    /// 下から通り抜けられる床(すり抜け床)。
    /// 配下のコライダーと各キャラクターの衝突を、キャラの足元が床の上面より
    /// 下にある間だけ無効化する。上から乗る時だけ床として機能する。
    /// </summary>
    public class OneWayPlatform : MonoBehaviour
    {
        const float SurfaceTolerance = 0.05f;
        const float CharacterRefreshInterval = 1.0f;

        Collider[] platformColliders = null;
        CharacterController[] characters = null;
        float refreshTimer = 0.0f;

        void Awake()
        {
            this.platformColliders = GetComponentsInChildren<Collider>();
        }

        void FixedUpdate()
        {
            RefreshCharacters();

            foreach (var character in this.characters)
            {
                if (character == null) continue;

                var feetY = character.bounds.min.y;

                foreach (var platformCollider in this.platformColliders)
                {
                    if (platformCollider == null) continue;

                    // 足元が上面より下にいる間は衝突を無効化(頭や体が引っかからない)
                    var ignore = feetY < platformCollider.bounds.max.y - SurfaceTolerance;
                    Physics.IgnoreCollision(platformCollider, character, ignore);
                }
            }
        }

        void RefreshCharacters()
        {
            this.refreshTimer -= Time.fixedDeltaTime;
            if (this.characters != null && this.refreshTimer > 0.0f) return;

            // リスポンで生成されるプレイヤーを拾うため定期的に取り直す
            this.characters = FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude);
            this.refreshTimer = CharacterRefreshInterval;
        }
    }
}
