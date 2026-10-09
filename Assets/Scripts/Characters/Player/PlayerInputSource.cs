using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rail
{
    public sealed class PlayerInputSource : IPlayerInputSource, IDisposable
    {
        const string PLAYER_MAP_NAME = "Player";
        const string MOVE_ACTION_NAME = "Move";
        const string JUMP_ACTION_NAME = "Jump";
        const string ATTACK_ACTION_NAME = "Attack";

        readonly InputActionMap playerMap;
        readonly InputAction moveAction;
        readonly InputAction jumpAction;
        readonly InputAction attackAction;

        public float Move => this.moveAction.ReadValue<Vector2>().x;
        public bool JumpPressed => this.jumpAction.WasPressedThisFrame();

        public bool JumpHeld => this.jumpAction.IsPressed();

        public bool AttackPressed => this.attackAction.WasPressedThisFrame();

        public PlayerInputSource(
            InputActionAsset inputActions)
        {
            this.playerMap = inputActions.FindActionMap(PLAYER_MAP_NAME, true);
            this.moveAction = this.playerMap.FindAction(MOVE_ACTION_NAME, true);
            this.jumpAction = this.playerMap.FindAction(JUMP_ACTION_NAME, true);
            this.attackAction = this.playerMap.FindAction(ATTACK_ACTION_NAME, true);
            this.playerMap.Enable();
        }

        void IDisposable.Dispose()
        {
            // Rootの所有物なのでここでは無効化するだけ。
            this.playerMap.Disable();
        }
    }
}
