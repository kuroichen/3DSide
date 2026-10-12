using System;
using UniRx;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public class PlayerCharacter : CharacterMover, IItemReceiver
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

        public IObservable<DamageInfo> OnAttackReceivedObservable() => this.onAttackReceivedSubject;
        public IObservable<Unit> OnDeathAnimationFinishedObservable() => this.onDeathAnimationFinishedSubject;
        public IObservable<Unit> OnInstantKillReceivedObservable() => this.onInstantKillReceivedSubject;
        public IObservable<ItemEffect> OnItemReceivedObservable() => this.onItemReceivedSubject;

        readonly Subject<DamageInfo> onAttackReceivedSubject = new();
        readonly Subject<Unit> onDeathAnimationFinishedSubject = new();
        readonly Subject<Unit> onInstantKillReceivedSubject = new();
        readonly Subject<ItemEffect> onItemReceivedSubject = new();

        IPlayerInputSource inputSource = null;

        PlayerState currentState = PlayerState.Move;
        float knockbackTimer = 0.0f;

        protected override float GetInput() => this.inputSource?.Move ?? 0.0f;
        protected override bool IsJumpPressed() => this.inputSource?.JumpPressed ?? false;
        protected override bool IsJumpHeld() => this.inputSource?.JumpHeld ?? false;

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            this.inputSource = inputSource;
        }

        public override void SetSpline(SplineContainer splineContainer, float t)
        {
            base.SetSpline(splineContainer, t);
            var tangent = this.Tangent;
            tangent.y = 0.0f;
            this.targetRotation = Quaternion.LookRotation(tangent.normalized);
        }


        #region Unity Events

        protected override void Awake()
        {
            base.Awake();
            
            this.onAttackReceivedSubject.AddTo(this);
            this.onDeathAnimationFinishedSubject.AddTo(this);
            this.onInstantKillReceivedSubject.AddTo(this);
            this.onItemReceivedSubject.AddTo(this);

            // 死亡アニメーションの完了通知の購読
            this.animationEventObserver.OnAnimationEventObservable()
                .Where(eventName => eventName == "OnDied")
                .Subscribe(_ => onDeathAnimationFinishedSubject.OnNext(Unit.Default))
                .AddTo(this);

            // プレイヤー自身の被弾判定
            this.collisionObserver.OnTriggerEnterObservable()
                .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                .Do(col => Debug.Log($"[PlayerController]OnTriggerEnter: {col.gameObject.name}"))
                .Subscribe(col =>
                {
                    var enemy = col.GetComponentInParent<CharacterMover>();
                    var damage = enemy != null
                        ? enemy.Entity.Attack
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
                            damage = this.Entity.Attack,
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

            this.onAttackReceivedSubject.OnNext(damageInfo);
        }

        /// <summary>
        /// ノックバックしてダメージステートへ
        /// </summary>
        /// <param name="attacker"></param>
        public void PlayHitReaction(Transform attacker)
        {
            if (this.currentState == PlayerState.Dead) return;

            // ノックバック
            AddImpulse(CalcKnockback(attacker));

            // ダメージステートへ
            ChangeState(PlayerState.Damage);
        }

        /// <summary>
        /// 死亡ステートへ移行
        /// </summary>
        public void Die()
        {
            ChangeState(PlayerState.Dead);
        }

        protected override void OnFellIntoPit()
        {
            // 奈落にいる間は毎フレーム呼ばれるので、死亡後は通知しない。
            if (this.currentState == PlayerState.Dead) return;

            this.onInstantKillReceivedSubject.OnNext(Unit.Default);
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
            
            if (this.inputSource?.AttackPressed == true)
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

        #region アイテム

        public void ReceiveItem(ItemEffect effect)
        {
            if (this.currentState == PlayerState.Dead)
            {
                return;
            }

            this.onItemReceivedSubject.OnNext(effect);
        }

        #endregion
    }
}
