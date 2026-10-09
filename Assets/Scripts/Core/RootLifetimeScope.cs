using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public class RootLifetimeScope : LifetimeScope
    {
        [SerializeField]
        InputActionAsset inputActions = null;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(this.inputActions);

            builder.Register<AudioManager>(Lifetime.Singleton);
            builder.Register<SceneLoader>(Lifetime.Singleton);
        }
    }
}
