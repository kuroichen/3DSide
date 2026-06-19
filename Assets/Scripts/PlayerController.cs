using System;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace Rail
{
    public class PlayerController : CharacterMover
    {
        [SerializeField]
        private CollisionObserver collisionObserver = null;

        [SerializeField]
        private CollisionObserver weaponCollisionObserver = null;

        [SerializeField]
        private AnimationEventObserver animationEventObserver = null;

        [SerializeField]
        float knockbackForce = 2.5f;

        [SerializeField]
        float knockbackUpForce = 3.0f;

        [SerializeField]
        float knockbackDuration = 0.3f;

        enum PlayerState
        {
            Move,
            Attack,
            Damage,
            Dead
        }

        InputAction moveInput = null;
        InputAction jumpInput = null;
        InputAction attackInput = null;

        PlayerState currentState = PlayerState.Move;
        float knockbackTimer = 0.0f;

        protected override float GetInput() => this.moveInput.ReadValue<Vector2>().x;
        protected override bool ShouldJump() => this.jumpInput.WasPressedThisFrame();

        public IObservable<Unit> OnDiedObservable()
        {
            return this.animationEventObserver.OnAnimationEventObservable()
                .Where(eventName => eventName == "OnDied")
                .AsUnitObservable();
        }

        public void SetSpline(SplineContainer splineContainer)
        {
            this.splineContainer = splineContainer;
            var tangent = (Vector3)this.splineContainer.EvaluateTangent(this.t);
            tangent.y = 0.0f;
            this.targetRotation = Quaternion.LookRotation(tangent.normalized);
        }


        #region Unity Events

        protected override void Awake()
        {
            base.Awake();
            this.moveInput = InputSystem.actions.FindAction("Move");
            this.jumpInput = InputSystem.actions.FindAction("Jump");
            this.attackInput = InputSystem.actions.FindAction("Attack");

            // プレイヤー自身の被弾判定
            this.collisionObserver.OnTriggerEnterObservable()
                .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                .Do(col => Debug.Log($"[PlayerController]OnTriggerEnter: {col.gameObject.name}"))
                .Subscribe(col =>
                {
                    var enemy = col.GetComponentInParent<CharacterMover>();
                    var damage = enemy != null
                        ? enemy.Attack.Attack
                        : 0;
                    TakeDamage(new DamageInfo()
                    {
                        attacker = col.transform,
                        damage = damage,
                    });
                })
                .AddTo(this);

            // 武器の攻撃判定
            this.weaponCollisionObserver.SetCollisionEnabled(false); // 初期状態は攻撃判定を無効化
            this.weaponCollisionObserver.OnTriggerEnterObservable()
                .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                .Do(col => Debug.Log($"[PlayerController]Weapon OnTriggerEnter: {col.gameObject.name}"))
                .Subscribe(col =>
                {
                    // 敵のTakeDamageを呼び出す。
                    var direction = col.transform.position - this.transform.position;
                    direction.y = 0.0f;
                    col.GetComponentInParent<IDamageable>()?.TakeDamage(
                        new DamageInfo
                        {
                            damage = this.runtimeStatus.Attack,
                            attacker = this.transform,
                        });
                })
                .AddTo(this);

            // AnimationEventの購読
            // 攻撃開始(攻撃判定を有効化する)
            this.animationEventObserver.OnAnimationEventObservable()
                .Where(eventName => eventName == "OnAttackStart")
                .Do(eventName => Debug.Log($"[PlayerController][AnimationEvent]{eventName}"))
                .Subscribe(_ => this.weaponCollisionObserver.SetCollisionEnabled(true))
                .AddTo(this);

            // 死亡アニメーション終了
            OnDiedObservable()
                .Do(_ => Debug.Log($"[PlayerController][AnimationEvent]OnDied"))
                .Subscribe(_ => Debug.Log("[PlayerController]死んだ！"))
                .AddTo(this);

            // 攻撃終了(攻撃判定を無効化する)
            this.animationEventObserver.OnAnimationEventObservable()
                .Where(eventName => eventName == "OnAttackEnd")
                .Do(eventName => Debug.Log($"[PlayerController][AnimationEvent]{eventName}"))
                .Subscribe(_ => 
                {
                    this.weaponCollisionObserver.SetCollisionEnabled(false);
                    ChangeState(PlayerState.Move);
                })
                .AddTo(this);

            // 死亡通知の購読
            this.runtimeStatus.OnDeadObservable()
                .Do(_ => Debug.Log($"[PlayerController]OnDead"))
                .Subscribe(_ => ChangeState(PlayerState.Dead))
                .AddTo(this);
        }

        protected override void Update()
        {
            switch (this.currentState)
            {
                case PlayerState.Move:
                    UpdateMove();
                    break;
                case PlayerState.Attack:
                    UpdateAttack();
                    break;
                case PlayerState.Damage:
                    UpdateDamage();
                    break;
                case PlayerState.Dead:
                    UpdateDead();
                    break;
            }
        }

        #endregion

        #region 被ダメ

        /// <summary>
        /// 被ダメージ処理
        /// </summary>
        /// <param name="damageInfo"></param>
        public override void TakeDamage(DamageInfo damageInfo)
        {
            if (this.currentState == PlayerState.Dead) return;

            // ダメージ付与
            this.runtimeStatus.TakeDamage(damageInfo.damage);

            if (this.currentState == PlayerState.Dead) return;

            // ノックバック
            AddImpulse(CalcKnockback(damageInfo.attacker));

            // ダメージステートへ
            ChangeState(PlayerState.Damage);
        }

        Vector3 CalcKnockback(Transform attacker)
        {
            var direction = this.transform.position - attacker.position;
            direction.y = 0.0f;
            return direction.normalized * this.knockbackForce + Vector3.up * this.knockbackUpForce;
        }

        #endregion

        #region ステート

        void ChangeState(PlayerState next)
        {
            if (this.currentState == PlayerState.Dead && next != PlayerState.Dead)
            {
                return;
            }

            this.currentState = next;

            Debug.Log($"[PlayerController][ChangeState] currentState: {this.currentState}");

            switch (next)
            {
                case PlayerState.Attack:
                    this.animator.SetTrigger("AttackTrigger");
                    break;
                case PlayerState.Damage:
                    this.knockbackTimer = this.knockbackDuration;
                    break;
                case PlayerState.Dead:
                    this.animator.SetTrigger("DeadTrigger");
                    this.collisionObserver.SetCollisionEnabled(false);
                    break;
            }
        }

        void UpdateMove()
        {
            base.Update();
            
            if (this.attackInput.WasPressedThisFrame())
            {
                ChangeState(PlayerState.Attack);
            }
        }

        void UpdateAttack()
        {
            base.Update();
        }

        void UpdateDamage()
        {
            base.Update();

            this.knockbackTimer -= Time.deltaTime;

            if (this.knockbackTimer <= 0.0f)
            {
                ChangeState(PlayerState.Move);
            }
        }

        void UpdateDead()
        {
            // 一定時間後にリスポン？
        }

        #endregion
    }
}
