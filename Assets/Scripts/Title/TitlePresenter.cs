using System;
using Cysharp.Threading.Tasks;
using UniRx;
using VContainer;

namespace Rail
{
    public class TitlePresenter : IDisposable
    {
        CompositeDisposable disposables = new();
        
        [Inject]
        public TitlePresenter(
            SceneLoader sceneLoader,
            ScreenTransition screenTransition,
            TitleView view)
        {
            view.OnClickStartButtonAsObservable()
                .Subscribe(async _ => await sceneLoader.LoadAsync("WorldScene"))
                .AddTo(this.disposables);

            screenTransition.FadeIn().Forget();
        }

        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
        }
    }
}
