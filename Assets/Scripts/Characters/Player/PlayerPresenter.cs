using System;
using UniRx;
using UnityEngine;
using VContainer;

namespace Rail
{
    public sealed class PlayerPresenter : IDisposable
    {
        readonly PlayerFactory playerFactory = null;
        readonly IPlayerInputSource inputSource = null;
        readonly SplineFollowCamera followCamera = null;
        
        readonly CompositeDisposable disposables = new();

        PlayerView playerView = null;

        [Inject]
        public PlayerPresenter(
            PlayerFactory playerFactory,
            IPlayerInputSource inputSource,
            SplineFollowCamera followCamera,
            IStageEventBus stageEventBus)
        {
            this.playerFactory = playerFactory;
            this.inputSource = inputSource;
            this.followCamera = followCamera;

            stageEventBus.OnPreparedObservable()
                .Subscribe(PlacePlayer)
                .AddTo(this.disposables);
        }

        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
            DestroyPlayer();
        }

        /// <summary>
        /// プレイヤーを指定した位置に配置する。
        /// プレイヤーが居なければ生成
        /// </summary>
        void PlacePlayer(SplineAnchor destinationPoint)
        {
            if (this.playerView == null)
            {
                this.CreatePlayer(destinationPoint);
                return;
            }

            this.playerView.PlaceAt(destinationPoint);
        }

        /// <summary>
        /// Playerを生成する。
        /// </summary>
        void CreatePlayer(SplineAnchor spawnPoint)
        {
            this.playerView = this.playerFactory.Create(spawnPoint);

            if (this.playerView == null) return;

            this.playerView.SetInputSource(this.inputSource);
            this.followCamera.SetTarget(this.playerView.Mover);
        }

        /// <summary>
        /// Playerを削除する。
        /// </summary>
        void DestroyPlayer()
        {
            if (this.playerView == null) return;

            if (this.followCamera != null)
            {
                this.followCamera.ClearTarget();
            }

            this.playerView.SetInputSource(null);
            GameObject.Destroy(this.playerView.gameObject);
            this.playerView = null;
        }
    }
}