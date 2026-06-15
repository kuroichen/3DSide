using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

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

        InputAction moveInput = null;
        InputAction jumpInput = null;
        InputAction attackInput = null;

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
                    var enemyProperties = col.GetComponent<ICharacterProperties>();
                    var damage = enemyProperties != null
                        ? enemyProperties.Attack.Attack
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
                    col.GetComponent<IDamageable>()?.TakeDamage(
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

            // 攻撃終了(攻撃判定を無効化する)
            this.animationEventObserver.OnAnimationEventObservable()
                .Where(eventName => eventName == "OnAttackEnd")
                .Do(eventName => Debug.Log($"[PlayerController][AnimationEvent]{eventName}"))
                .Subscribe(_ => this.weaponCollisionObserver.SetCollisionEnabled(false))
                .AddTo(this);
        }

        protected override void Update()
        {
            base.Update();

            if (this.attackInput.WasPressedThisFrame())
            {
                this.animator.SetTrigger("AttackTrigger");
            }
        }

        public override void TakeDamage(DamageInfo damageInfo)
        {
            var direction = this.transform.position - damageInfo.attacker.position;
            direction.y = 0.0f;
            AddImpulse(direction.normalized * this.knockbackForce + Vector3.up * this.knockbackUpForce);
            
            this.runtimeStatus.TakeDamage(damageInfo.damage);
        }

        protected override float GetInput()
        {
            return this.moveInput.ReadValue<Vector2>().x;
        }

        protected override bool ShouldJump()
        {
            return this.jumpInput.WasPressedThisFrame();
        }
    }
}
