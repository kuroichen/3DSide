using UnityEngine;
using UnityEngine.Splines;

namespace Rail
{
    public class CharacterMover : SplineMover
    {
        [SerializeField]
        float jumpForce = 8.0f;

        [SerializeField]
        float gravity = -20.0f;

        protected CharacterController characterController;
        float verticalVelocity = 0.0f;


        protected override void Awake()
        {
            base.Awake();
            this.characterController = GetComponent<CharacterController>();
        }

        protected override void Update()
        {
            base.Update();

            // 向きはSplineMoverの接線を使用
            if (this.Tangent != Vector3.zero)
            {
                this.transform.rotation = Quaternion.LookRotation(this.Tangent);
            }

            ApplyToCharacterController();
        }

        /// <summary>
        /// ジャンプすべきか。
        /// 子クラスで上書きする。
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldJump() => false;

        void ApplyToCharacterController()
        {
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
                maxDistance: xzMove.magnitude + 0.1f);

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
                this.verticalVelocity = 0.0f;
            }

            if (this.characterController.isGrounded)
            {
                // 接地中は小さな下向きの力で地面に張り付かせる
                this.verticalVelocity = -1.0f;

                if (ShouldJump())
                {
                    this.verticalVelocity = jumpForce;
                }
            }
            else
            {
                // 空中は重力を加算
                this.verticalVelocity += gravity * Time.deltaTime;
            }

            yMove = this.verticalVelocity * Time.deltaTime;
        }
    }
}
