using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public class WorldLifetimeScope : LifetimeScope
    {
        [SerializeField]
        WorldView view = null;
        
        protected override void Configure(IContainerBuilder builder)
        {
             builder.RegisterComponent(view);

             builder.Register<WorldLoader>(Lifetime.Singleton);
             builder.RegisterPlainEntryPoint<WorldPresenter>();
        }
    }
}
