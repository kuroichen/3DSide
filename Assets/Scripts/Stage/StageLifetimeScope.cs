using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Rail
{
    public class StageLifetimeScope : LifetimeScope
    {
        [SerializeField]
        PlayerView playerPrefab = null;

        [SerializeField]
        SplineFollowCamera followCamera = null;

        [SerializeField]
        CharacterStatus playerStatus = null;

        [SerializeField]
        StageHUDView hudView = null;

        [SerializeField]
        int initialLives = 2;

        protected override void Configure(IContainerBuilder builder)
        {
            // HUD
            builder.RegisterComponent<StageHUDView>(this.hudView);

            // Stage
            builder.RegisterComponent<SplineFollowCamera>(this.followCamera);
            builder.Register<StageLoader>(Lifetime.Scoped);
            builder.Register<StageModel>(Lifetime.Scoped);
            builder.Register<StageDataStore>(Lifetime.Scoped);
            builder.Register<EnemyFactory>(Lifetime.Scoped);
            builder.Register<StageEventBus>(Lifetime.Scoped)
                .AsSelf()
                .As<IStageEventBus>();

            // Player
            builder.RegisterInstance<PlayerView>(this.playerPrefab);
            builder.Register<PlayerInputSource>(Lifetime.Scoped).As<IPlayerInputSource>();
            builder.Register<PlayerDataStore>(Lifetime.Scoped);
            builder.Register<PlayerModel>(Lifetime.Scoped);
            builder.Register<PlayerFactory>(Lifetime.Scoped);
            builder.Register<PlayerEventBus>(Lifetime.Scoped)
                .AsSelf()
                .As<IPlayerEventBus>();
            builder.RegisterInstance(new PlayerEntity(
                hp: this.playerStatus.hp,
                maxHp: this.playerStatus.hp,
                attack: this.playerStatus.attack,
                speed: this.playerStatus.speed,
                jumpForce: this.playerStatus.jumpForce,
                lives: this.initialLives));
        
            // EntryPoint
            builder.RegisterPlainEntryPoint<PlayerPresenter>(Lifetime.Scoped);
            builder.RegisterPlainEntryPoint<StagePresenter>(Lifetime.Scoped);
            builder.RegisterPlainEntryPoint<StageHUDPresenter>(Lifetime.Scoped);
        }
    }
}
