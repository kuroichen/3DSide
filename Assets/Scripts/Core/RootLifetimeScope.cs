using UnityEngine;
using UnityEngine.EventSystems;
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

        [SerializeField]
        EventSystem eventSystem = null;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance<InputActionAsset>(this.inputActions);
            builder.RegisterComponent<ScreenTransition>(this.screenTransition);
            builder.RegisterComponent<AudioManager>(this.audioManager);
            builder.RegisterComponent<EventSystem>(this.eventSystem);

            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<InputBlocker>(Lifetime.Singleton);
        }
    }
}
