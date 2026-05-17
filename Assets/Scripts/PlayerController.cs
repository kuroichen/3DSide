using UnityEngine;
using UnityEngine.InputSystem;

namespace Rail
{
    public class PlayerController : CharacterMover
    {
        InputAction moveInput = null;
        InputAction jumpInput = null;

        protected override void Awake()
        {
            base.Awake();
            this.moveInput = InputSystem.actions.FindAction("Move");
            this.jumpInput = InputSystem.actions.FindAction("Jump");
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
