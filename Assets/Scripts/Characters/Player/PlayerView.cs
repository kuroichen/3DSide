using UnityEngine;
using System;
using UniRx;
using VContainer;

namespace Rail
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField]
        PlayerController playerController = null;

        [SerializeField]
        CharacterController characterController = null;

        public IObservable<Unit> OnDeadObservable() => this.playerController.LifeProperty.OnDeadObservable();
        public IObservable<Unit> OnDeathAnimationFinishedObservable() => this.playerController.OnDiedObservable();

        public SplineMover Mover => this.playerController;
        
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

            this.OnDeadObservable()
                .Subscribe(_ => this.playerEventBus.PublishDead())
                .AddTo(this);

            this.OnDeathAnimationFinishedObservable()
                .Subscribe(_ => this.playerEventBus.PublishDeathAnimationFinished())
                .AddTo(this);

            // 生成できたことを通知
            this.playerEventBus.PublishSpawned();
        }

        public void PlaceAt(SplineAnchor anchor)
        {
            if (anchor == null) return;

            var wasEnabled = this.characterController.enabled;
            this.characterController.enabled = false;

            this.playerController.SetSpline(anchor.SplineContainer, anchor.T);

            this.transform.SetPositionAndRotation(
                anchor.transform.position,
                anchor.GetRotation()
            );

            this.characterController.enabled = wasEnabled;
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            this.playerController.SetInputSource(inputSource);
        }

        void Reset()
        {
            this.playerController = this.GetComponent<PlayerController>();

            this.characterController = this.GetComponent<CharacterController>();
        }

    }
}
