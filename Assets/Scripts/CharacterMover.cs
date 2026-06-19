using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public abstract class CharacterMover : SplineMover, IDamageable
    {
        [SerializeField]
        protected CharacterStatus defaultStatus = null;

        [SerializeField]
        float gravity = -20.0f;

        [SerializeField]
        float rotationSpeed = 10.0f;

        // パラメータ
        protected CharacterStatusRuntime runtimeStatus = null;

        // 移動関連
        protected CharacterController characterController = null;

        protected override float Speed => this.runtimeStatus.Speed;

        // -----------------------------
        // 外部公開用
        // -----------------------------

        public IAttack Attack => this.runtimeStatus;
        public ILife Life => this.runtimeStatus;

        // -----------------------------

        protected Quaternion targetRotation = Quaternion.identity;
        float verticalVelocity = 0.0f;
        bool wasGrounded = false;
        int wallLayerMask = 0;


        protected override void Awake()
        {
            base.Awake();
            this.characterController = GetComponent<CharacterController>();
            this.wallLayerMask = LayerMask.GetMask("Terrain", "Gimmick");

            this.runtimeStatus = new CharacterStatusRuntime(this.defaultStatus);
        }

        protected override void Update()
        {
            base.Update();

            UpdateRotation();

            ApplyToCharacterController();
        }

        public abstract void TakeDamage(DamageInfo damageInfo);

        public void AddImpulse(Vector3 worldImpulse)
        {
            var length = this.splineContainer.CalculateLength();

            // XZ成分をSplineの接線との内積でt速度に変換
            // 内積が正→進行方向（t+）, 負→逆方向（t-）
            var tangentXZ = new Vector3(this.Tangent.x, 0.0f, this.Tangent.z).normalized;
            var dot = Vector3.Dot(worldImpulse, tangentXZ);
            this.impulseVelocityT += dot / (this.speed / length);

            // Y成分は垂直速度に直接加算
            this.verticalVelocity += worldImpulse.y;
        }

        /// <summary>
        /// ジャンプすべきか。
        /// 子クラスで上書きする。
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldJump() => false;

        void UpdateRotation()
        {
            if (this.Tangent == Vector3.zero)
            {
                return;
            }

            var input = GetInput();

            if (input != 0.0f)
            {
                // 入力方向に応じて接線の向きを決める
                // 交代時は接戦を反転させて後ろを向く
                var lookDirection = input > 0.0f 
                    ? this.Tangent 
                    : -this.Tangent;
                this.targetRotation = Quaternion.LookRotation(lookDirection);
            }

            // 入力がなくても補間は続ける（向き変更の途中で止まらないように）
            this.transform.rotation = Quaternion.Slerp(
                this.transform.rotation,
                this.targetRotation,
                this.rotationSpeed * Time.deltaTime);
        }

        void ApplyToCharacterController()
        {
            if (this.characterController == null
                || !this.characterController.enabled
                || !this.characterController.gameObject.activeInHierarchy)
            {
                return;
            }

            // XZ方向の移動量を計算
            // 現在位置とスプライン目標位置の差分をCCに渡す
            Vector3 current = this.transform.position;
            Vector3 xzMove = new Vector3(
                x: this.Position.x - current.x,
                y: 0.0f,
                z: this.Position.z - current.z);

            ApplyGravityAndJump(out float yMove);

            // 移動前にCapsuleCastで先読み
            var blocked = Physics.CapsuleCast(
                point1: this.transform.position + Vector3.up
                    * (this.characterController.height - this.characterController.radius),
                point2: this.transform.position + Vector3.up
                    * this.characterController.radius,
                radius: this.characterController.radius,
                direction: xzMove.normalized,
                maxDistance: xzMove.magnitude + 0.1f,
                layerMask: this.wallLayerMask);

            if (blocked)
            {
                // 壁がある方向にはtを戻す
                RevertT();
            }

            // CharacterControllerで移動
            this.characterController.Move(
                new Vector3(
                    x: xzMove.x,
                    y: yMove,
                    z: xzMove.z));
        }

        void ApplyGravityAndJump(out float yMove)
        {
            // 天井に当たったら上昇を止める
            if (this.characterController.collisionFlags.HasFlag(CollisionFlags.Above))
            {
                this.verticalVelocity = -1.0f;
            }

            if (this.characterController.isGrounded)
            {
                // 着地した瞬間を検知
                if (!wasGrounded)
                {
                    OnLanded();
                }

                // 接地中は小さな下向きの力で地面に張り付かせる
                // ただしAddImpulseで上昇中の場合は上書きしない
                if (this.verticalVelocity <= 0.0f)
                {
                    this.verticalVelocity = -1.0f;
                }

                if (ShouldJump())
                {
                    this.verticalVelocity = this.runtimeStatus.JumpForce;
                    OnJumped(); // ジャンプトリガー
                }

                this.wasGrounded = true;
            }
            else
            {
                this.wasGrounded = false;

                // 空中は重力を加算
                this.verticalVelocity += gravity * Time.deltaTime;
            }

            if (this.animator != null)
            {
                this.animator.SetBool("IsGrounded", this.characterController.isGrounded);
            }

            yMove = this.verticalVelocity * Time.deltaTime;
        }



        void OnJumped()
        {
            if (this.animator != null)
            {
                this.animator.SetTrigger("JumpTrigger");
            }
        }

        void OnLanded()
        {
            if (this.animator != null)
            {
                this.animator.SetBool("IsGrounded", true);
            }
        }
    }
}
