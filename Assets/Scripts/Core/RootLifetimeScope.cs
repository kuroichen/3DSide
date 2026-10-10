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

        [SerializeField]
        AudioManager audioManager = null;

        [SerializeField]
        ScreenTransition screenTransition = null;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<InputActionAsset>(this.inputActions);
            builder.RegisterComponent<ScreenTransition>(this.screenTransition);
            builder.RegisterComponent<AudioManager>(this.audioManager);

            builder.Register<SceneLoader>(Lifetime.Singleton);
        }
    }
}
