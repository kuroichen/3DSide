using System;
using Cysharp.Threading.Tasks;
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
        readonly PlayerModel playerModel = null;
        readonly PlayerEventBus playerEventBus = null;
        readonly ScreenTransition screenTransition = null;
        
        readonly CompositeDisposable disposables = new();

        PlayerView playerView = null;

        [Inject]
        public PlayerPresenter(
            PlayerFactory playerFactory,
            IPlayerInputSource inputSource,
            SplineFollowCamera followCamera,
            PlayerModel playerModel,
            PlayerEventBus playerEventBus,
            IStageEventBus stageEventBus,
            ScreenTransition screenTransition)
        {
            this.playerFactory = playerFactory;
            this.inputSource = inputSource;
            this.followCamera = followCamera;
            this.playerModel = playerModel;
            this.playerEventBus = playerEventBus;
            this.screenTransition = screenTransition;

            stageEventBus.OnPreparedObservable()
                .Subscribe(PlacePlayer)
                .AddTo(this.disposables);

            stageEventBus.OnRespawnRequestObservable()
                .Subscribe(RespawnPlayer)
                .AddTo(this.disposables);

            playerEventBus.OnSpawnedObservable()
                .Subscribe(_ => this.screenTransition.FadeIn().Forget())
                .AddTo(this.disposables);

            playerEventBus.OnDeathAnimationFinishedObservable()
                .Subscribe(_ => HandleDeathAsync().Forget())
                .AddTo(this.disposables);

            playerEventBus.OnAttackReceivedObservable()
                .Subscribe(ApplyDamage)
                .AddTo(this.disposables);

            playerEventBus.OnItemReceivedObservable()
                .Subscribe(ApplyItem)
                .AddTo(this.disposables);

            playerEventBus.OnInstantKillReceivedObservable()
                .Subscribe(_ => ApplyInstantKill())
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

            this.playerView.SetEntity(this.playerModel.Entity);

            this.playerView.SetInputSource(this.inputSource);
            this.followCamera.SetTarget(this.playerView.Mover);

            // 生成し直したらHPは最大から
            this.playerModel.ResetHp();
        }

        /// <summary>
        /// 受けた攻撃をModelに反映し、結果に応じてのけぞりか死亡をViewに指示する。
        /// </summary>
        /// <param name="damageInfo"></param>
        void ApplyDamage(DamageInfo damageInfo)
        {
            switch (this.playerModel.Damage(damageInfo.damage))
            {
                case DamageResult.Ignored:
                    break;
                case DamageResult.Damaged:
                    this.playerView.PlayHitReaction(damageInfo.attacker);
                    break;
                case DamageResult.Died:
                    Die();
                    break;
                default:
                    Debug.LogError("[PlayerPresenter] [ApplyDamage] unknown DamageResult.");
                    break;
            }
        }

        /// <summary>
        /// 即死を受けたら、HPに関係なく死亡させる。のけぞりは出さない。
        /// </summary>
        void ApplyInstantKill()
        {
            if (!this.playerModel.Kill()) return;

            Die();
        }

        /// <summary>
        /// 受け取ったアイテムの効果をModelに反映する。
        /// </summary>
        void ApplyItem(ItemEffect effect)
        {
            switch (effect.Type)
            {
                case ItemEffectType.Heal:
                    this.playerModel.Heal(effect.Amount);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 死亡を体に指示。
        /// </summary>
        void Die()
        {
            this.playerView.Die();
            this.playerEventBus.PublishDead();
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

        /// <summary>
        /// 死亡アニメーション終了後に残機を消費し、結果を通知する。
        /// </summary>
        void ConsumeLife()
        {
            if (this.playerModel.TryConsumeLife())
            {
                this.playerEventBus.PublishLifeConsumed();
                return;
            }

            this.playerEventBus.PublishLivesDepleted();
        }

        /// <summary>
        /// Playerを作りなおして指定地点に配置する。
        /// 状態のリセット漏れを防ぐため、移動ではなく再生成する。
        /// </summary>
        void RespawnPlayer(SplineAnchor spawnPoint)
        {
            DestroyPlayer();
            CreatePlayer(spawnPoint);
        }

        /// <summary>
        /// 死亡時のハンドリング
        /// </summary>
        async UniTask HandleDeathAsync()
        {
            await this.screenTransition.FadeOut();

            ConsumeLife();
        }
    }
}