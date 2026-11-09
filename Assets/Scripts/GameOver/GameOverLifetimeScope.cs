using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public sealed class GameOverLifetimeScope : LifetimeScope
    {
        [SerializeField]
        GameOverView gameOverView = null;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<GameOverView>(this.gameOverView);

            builder.RegisterPlainEntryPoint<GameOverPresenter>(Lifetime.Singleton);
        }
    }
}
