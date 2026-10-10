using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public class TitleLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private TitleView view = null;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<TitleView>(view);
            builder.RegisterPlainEntryPoint<TitlePresenter>(Lifetime.Singleton);
        }
    }
}
