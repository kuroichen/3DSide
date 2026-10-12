using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public abstract class CharacterMover : SplineMover, IDamageable, ICharacterProperties
    {
        [SerializeField]
        protected CharacterStatus defaultStatus = null;

        // 非対称重力: 上昇はゆっくり減速し、下降は速く落とす(レトロアクション系のキレのあるジャンプ)
        // リファレンス値(30fpsフレーム単位): jumpForce 26 / gravityUp 2 / gravityDown 3
        // scaleJumpByHeightが有効な場合、この2つは身長から自動導出されて上書きされる
        [SerializeField]
        float gravityUp = -36.0f;

        [SerializeField]
        float gravityDown = -54.0f;

        // --- ジャンプの身長スケーリング ---
        // 「身長の何倍・何秒で跳ぶか」で指定し、実際の重力・初速は
        // CharacterController.heightから導出する。モデルの頭身を変えても、
        // 時間(手触り)を保ったまま飛距離だけが身の丈に合う
        [SerializeField]
        bool scaleJumpByHeight = true;

        [SerializeField]
        float jumpHeightInBodies = 1.67f; // ジャンプ高さ(身長比)。現行の高さ3.34m/身長2.0mと等価

        [SerializeField]
        float timeToApex = 0.43f; // 上昇時間(秒)。手触りの正体なので身長では変えない

        [SerializeField]
        float fallGravityRatio = 1.5f; // 下降重力/上昇重力(落下のキレ)

        [SerializeField]
        float rotationSpeed = 10.0f;

        [SerializeField]
        float fallDeathY = -20.0f; // これより下に落ちたら死亡する高さ(奈落)

        // --- ジャンプの手触り(ジャンル標準のテクニック群) ---

        [SerializeField]
        float jumpCutMultiplier = 0.4f; // ボタンを離した瞬間に上昇速度へ掛ける係数(可変ジャンプ)

        [SerializeField]
        float coyoteTime = 0.1f; // 接地を離れてからもジャンプを受け付ける猶予(秒)

        [SerializeField]
        float jumpBufferTime = 0.15f; // 着地前の先行入力を覚えておく時間(秒)

        [SerializeField]
        float maxFallSpeed = 20.0f; // 落下速度の上限(終端速度)

        [SerializeField]
        float hardLandFallDistance = 4.0f; // これ以上の高さを落ちたら着地硬直(フルジャンプの落下3.34mでは発生しない)

        [SerializeField]
        float hardLandStunTime = 0.25f; // 着地硬直の時間(秒)

        [SerializeField]
        float railCorrectionSpeed = 15.0f; // レール目標位置へ寄せる最大速度(乗り換え時のワープ・ガクつき対策)

        [SerializeField]
        float groundCheckDistance = 0.15f; // 足元からこの距離以内に地面があれば接地とみなす

        // パラメータ
        protected CharacterStatusRuntime runtimeStatus = null;

        // 移動関連
        protected CharacterController characterController = null;

        protected override float Speed => this.runtimeStatus.Speed;

        // -----------------------------
        // 外部公開用
        // -----------------------------

        public IAttack AttackProperty => this.runtimeStatus;
        public ILife LifeProperty => this.runtimeStatus;

        // -----------------------------

        protected Quaternion targetRotation = Quaternion.identity;
        float verticalVelocity = 0.0f;
        bool wasGrounded = false;
        int wallLayerMask = 0;

        // ジャンプの手触り用
        float coyoteTimer = 0.0f;
        float jumpBufferTimer = 0.0f;
        bool canCutJump = false;
        float derivedJumpVelocity = 0.0f;

        // 着地硬直用
        float airborneTopY = 0.0f;
        float landingStunTimer = 0.0f;

        protected bool IsLandingStunned => this.landingStunTimer > 0.0f;
        protected override float InputScale => IsLandingStunned ? 0.0f : 1.0f;

        // 直近の安定接地判定(CheckGrounded)の結果を使う。CC素のisGroundedだと
        // 端の角で毎フレーム反転し、レール余白の伸縮が振動の原因になる
        protected override bool IsAirborne => !this.wasGrounded;

        // 身長スケーリング有効時は導出値、無効時はステータスの値を使う
        protected float JumpVelocity => this.scaleJumpByHeight
            ? this.derivedJumpVelocity
            : this.runtimeStatus.JumpForce;


        protected override void Awake()
        {
            base.Awake();
            this.characterController = GetComponent<CharacterController>();
            this.wallLayerMask = LayerMask.GetMask("Terrain", "Gimmick");

            this.runtimeStatus = new CharacterStatusRuntime(this.defaultStatus);

            SetupJumpProfile();
        }

        /// <summary>
        /// 身長(CharacterController.height)からジャンプの重力・初速を導出する。
        /// </summary>
        void SetupJumpProfile()
        {
            if (!this.scaleJumpByHeight) return;

            var apex = Mathf.Max(0.01f, this.timeToApex);
            var jumpHeight = this.jumpHeightInBodies * this.characterController.height;

            this.gravityUp = -2.0f * jumpHeight / (apex * apex);
            this.gravityDown = this.gravityUp * this.fallGravityRatio;
            this.derivedJumpVelocity = 2.0f * jumpHeight / apex;
        }

        protected override void Update()
        {
            base.Update();

            UpdateRotation();

            ApplyToCharacterController();

            CheckFallDeath();
        }

        /// <summary>
        /// 接地判定。
        /// CharacterController.isGroundedはカプセルの縁が足場の角にかすっただけでtrueになり、
        /// 端で毎フレーム反転してエッジハング(角に引っかかって降りられない)を起こす。
        /// カプセル中心の直下に本当に地面があるかをSphereCastで併せて確認する。
        /// </summary>
        bool CheckGrounded()
        {
            if (!this.characterController.isGrounded) return false;

            var origin = this.transform.position + this.characterController.center;
            var radius = this.characterController.radius * 0.9f;
            var castDistance = this.characterController.center.y - radius + this.groundCheckDistance;

            return Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out _,
                castDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 奈落への落下死判定。
        /// </summary>
        void CheckFallDeath()
        {
            if (this.transform.position.y > this.fallDeathY) return;

            OnFellIntoPit();
        }

        public abstract void TakeDamage(DamageInfo damageInfo);

        public void AddImpulse(Vector3 worldImpulse)
        {
            // XZ成分をSplineの接線方向入力に変換
            // 内積が正→進行方向、負→逆方向
            var tangentXZ = new Vector3(this.Tangent.x, 0.0f, this.Tangent.z).normalized;
            var dot = Vector3.Dot(worldImpulse, tangentXZ);
            this.impulseVelocityT += dot / Mathf.Max(0.001f, this.Speed);

            // Y成分は垂直速度に直接加算
            this.verticalVelocity += worldImpulse.y;

            // 外力による上昇は可変ジャンプの減衰対象にしない
            this.canCutJump = false;
        }

        /// <summary>
        /// この瞬間にジャンプボタンが押されたか。子クラスで上書きする。
        /// </summary>
        protected virtual bool IsJumpPressed() => false;

        /// <summary>
        /// ジャンプボタンが押されている最中か。子クラスで上書きする(可変ジャンプ用)。
        /// </summary>
        protected virtual bool IsJumpHeld() => false;

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

            // レール乗り換えなどで目標位置が飛んだ時、1フレームで吸い付かせず
            // 上限速度で滑らかに寄せる(瞬間ワープ・接地フリッカーの防止)
            var maxStep = this.railCorrectionSpeed * Time.deltaTime;
            if (xzMove.magnitude > maxStep)
            {
                xzMove = xzMove.normalized * maxStep;
            }

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

            var isGrounded = CheckGrounded();

            // コヨーテタイム: 接地中は満タン、離れたら減っていく
            this.coyoteTimer = isGrounded
                ? this.coyoteTime
                : this.coyoteTimer - Time.deltaTime;

            // ジャンプバッファ: 押した瞬間から一定時間覚えておく
            this.jumpBufferTimer -= Time.deltaTime;
            if (IsJumpPressed())
            {
                this.jumpBufferTimer = this.jumpBufferTime;
            }

            this.landingStunTimer -= Time.deltaTime;

            if (isGrounded)
            {
                // 着地した瞬間を検知
                if (!wasGrounded)
                {
                    // 落下距離(空中の最高到達点から)でハード着地かを判定
                    var fallDistance = this.airborneTopY - this.transform.position.y;
                    var isHardLand = fallDistance >= this.hardLandFallDistance;
                    if (isHardLand)
                    {
                        this.landingStunTimer = this.hardLandStunTime;
                    }
                    if (this.animator != null)
                    {
                        this.animator.SetBool("HardLand", isHardLand);
                    }

                    OnLanded();
                }

                this.airborneTopY = this.transform.position.y;

                // 接地中は小さな下向きの力で地面に張り付かせる
                // ただしAddImpulseで上昇中の場合は上書きしない
                if (this.verticalVelocity <= 0.0f)
                {
                    this.verticalVelocity = -1.0f;
                }

                this.wasGrounded = true;
            }
            else
            {
                this.wasGrounded = false;

                // 落下距離の計測用に最高到達点を記録
                this.airborneTopY = Mathf.Max(this.airborneTopY, this.transform.position.y);

                // 空中は重力を加算(上昇中と下降中で強さを切り替える)
                var currentGravity = this.verticalVelocity > 0.0f
                    ? this.gravityUp
                    : this.gravityDown;
                this.verticalVelocity += currentGravity * Time.deltaTime;

                // 終端速度でクランプ
                this.verticalVelocity = Mathf.Max(this.verticalVelocity, -this.maxFallSpeed);
            }

            // ジャンプ発火: バッファ(先行入力)とコヨーテ(崖離れ猶予)の両方が生きている時。
            // 着地硬直中は受け付けない
            if (this.jumpBufferTimer > 0.0f && this.coyoteTimer > 0.0f && !IsLandingStunned)
            {
                this.verticalVelocity = this.JumpVelocity;
                this.canCutJump = true;
                this.jumpBufferTimer = 0.0f;
                this.coyoteTimer = 0.0f;
                OnJumped(); // ジャンプトリガー
            }

            // 可変ジャンプ: 上昇中にボタンを離したら上昇を殺す(1回だけ)
            if (this.canCutJump && this.verticalVelocity > 0.0f && !IsJumpHeld())
            {
                this.verticalVelocity *= this.jumpCutMultiplier;
                this.canCutJump = false;
            }
            if (this.verticalVelocity <= 0.0f)
            {
                this.canCutJump = false;
            }

            if (this.animator != null)
            {
                this.animator.SetBool("IsGrounded", isGrounded);
                this.animator.SetFloat("VerticalVelocity", this.verticalVelocity);
            }

            yMove = this.verticalVelocity * Time.deltaTime;
        }

        /// <summary>
        /// 奈落に落ちた時の処理。規定では残りHPぶんのダメージを受けて死亡する。
        /// </summary>
        protected virtual void OnFellIntoPit()
        {
            if (this.runtimeStatus.CurrentHp <= 0)
            {
                return;
            }

            TakeDamage(new DamageInfo
            {
               damage = this.runtimeStatus.CurrentHp,
               attacker = this.transform, 
            });
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
