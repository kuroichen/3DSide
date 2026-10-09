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

        protected override void Configure(IContainerBuilder builder)
        {
            // Stage
            builder.RegisterInstance<SplineFollowCamera>(this.followCamera);
            builder.Register<StageLoader>(Lifetime.Scoped);
            builder.Register<StageModel>(Lifetime.Scoped);
            builder.Register<StageDataStore>(Lifetime.Scoped);
            builder.Register<StageEventBus>(Lifetime.Scoped)
                .AsSelf()
                .As<IStageEventBus>();

            // Player
            builder.RegisterInstance<PlayerView>(this.playerPrefab);
            builder.Register<PlayerInputSource>(Lifetime.Scoped).As<IPlayerInputSource>();
            builder.Register<PlayerFactory>(Lifetime.Scoped);
            builder.Register<PlayerEventBus>(Lifetime.Scoped)
                .AsSelf()
                .As<IPlayerEventBus>();
        
            // EntryPoint
            builder.RegisterPlainEntryPoint<PlayerPresenter>(Lifetime.Scoped);
            builder.RegisterPlainEntryPoint<StagePresenter>(Lifetime.Scoped);
        }
    }
}
