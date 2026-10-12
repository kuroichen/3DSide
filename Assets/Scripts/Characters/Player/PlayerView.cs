using UnityEngine;
using System;
using UniRx;
using VContainer;

namespace Rail
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField]
        PlayerCharacter playerCharacter = null;

        [SerializeField]
        CharacterController characterController = null;

        public IObservable<Unit> OnDeathAnimationFinishedObservable() => this.playerCharacter.OnDeathAnimationFinishedObservable();
        public IObservable<DamageInfo> OnAttackReceivedObservable() => this.playerCharacter.OnAttackReceivedObservable();
        public IObservable<Unit> OnInstantKillReceivedObservable() => this.playerCharacter.OnInstantKillReceivedObservable();
        public IObservable<ItemEffect> OnItemReceivedObservable() => this.playerCharacter.OnItemReceivedObservable();

        public SplineMover Mover => this.playerCharacter;
        
        PlayerEventBus playerEventBus = null;

        [Inject]
        public void Construct(PlayerEventBus playerEventBus)
        {
            this.playerEventBus = playerEventBus;
        }

        void Start()
        {
            // DI移行前のSceneから生成された場合は何もしない。
            if (this.playerEventBus == null) return;

            this.OnDeathAnimationFinishedObservable()
                .Subscribe(_ => this.playerEventBus.PublishDeathAnimationFinished())
                .AddTo(this);
            
            this.OnAttackReceivedObservable()
                .Subscribe(this.playerEventBus.PublishAttackReceived)
                .AddTo(this);

            this.OnInstantKillReceivedObservable()
                .Subscribe(_ => this.playerEventBus.PublishInstantKillReceived())
                .AddTo(this);

            this.OnItemReceivedObservable()
                .Subscribe(this.playerEventBus.PublishItemReceived)
                .AddTo(this);

            // 生成できたことを通知
            this.playerEventBus.PublishSpawned();
        }

        public void PlayHitReaction(Transform attacker) => this.playerCharacter.PlayHitReaction(attacker);
        public void Die() => this.playerCharacter.Die();


        public void PlaceAt(SplineAnchor anchor)
        {
            if (anchor == null) return;

            var wasEnabled = this.characterController.enabled;
            this.characterController.enabled = false;

            this.playerCharacter.SetSpline(anchor.SplineContainer, anchor.T);

            this.transform.SetPositionAndRotation(
                anchor.transform.position,
                anchor.GetRotation()
            );

            this.characterController.enabled = wasEnabled;
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            this.playerCharacter.SetInputSource(inputSource);
        }

        void Reset()
        {
            this.playerCharacter = this.GetComponent<PlayerCharacter>();

            this.characterController = this.GetComponent<CharacterController>();
        }

    }
}
