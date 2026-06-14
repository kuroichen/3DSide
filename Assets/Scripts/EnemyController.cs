using UnityEngine;

namespace Rail
{
    public class EnemyController : CharacterMover
    {
        [SerializeField]
        float knockbackDuration = 0.3f; // ノックバック継続時間

        float direction = 1.0f;

        enum EnemyState
        {
            Patrol,
            Damage,
            Dead
        }

        EnemyState currentState = EnemyState.Patrol;
        float knockbackTimer = 0.0f;

        protected override void Update()
        {
            switch (this.currentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Damage:
                    UpdateDamage();
                    break;
                case EnemyState.Dead:
                    UpdateDead();
                    break;
                default:
                    break;
            }
        }

        protected override float GetInput()
        {
            // パトロール中以外は入力しない
            if (this.currentState != EnemyState.Patrol) return 0.0f;

            if (this.t >= 1.0f && this.direction > 0.0f)
            {
                this.direction = -1.0f;
            }
            else if (this.t <= 0.0f && this.direction < 0.0f)
            {
                this.direction = 1.0f;
            }

            return this.direction;
        }


        #region ステート別Update

        void UpdatePatrol()
        {
            // 通常のCharacterMover更新（移動・重力・回転）
            base.Update();
        }

        void UpdateDamage()
        {
            // ノックバック中はbase.Update()でAddImpluseの減衰と重力を処理しつつ、
            // GetInput()は0を返して入力による移動を発生させない。
            base.Update();

            this.knockbackTimer -= Time.deltaTime;
            //Debug.Log($"[Enemy]knockbackTimer: {this.knockbackTimer}");
            if (this.knockbackTimer <= 0.0f)
            {
                ChangeState(EnemyState.Patrol);
                Debug.Log("[Enemy]Patrolに戻った");
            }
        }

        void UpdateDead()
        {
            // 死亡処理（現状は何もしない。将来的にアニメーション再生→オブジェクト削除など）
        }

        #endregion

        #region ステート管理
        void ChangeState(EnemyState next)
        {
            this.currentState = next;

            switch (next)
            {
                case EnemyState.Damage:
                    this.knockbackTimer = this.knockbackDuration;
                    break;
                default:
                    break;
            }
        }


        #endregion

        #region IDamageable

        public override void TakeDamage(DamageInfo damageInfo)
        {
            if (this.currentState == EnemyState.Dead) return;

            AddImpulse(damageInfo.knockback);
            ChangeState(EnemyState.Damage);

            // TODO ダメージ処理
        }

        #endregion
    }
}
