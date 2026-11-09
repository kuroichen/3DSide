using System;
using UniRx;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Rail
{
    public sealed class GameOverPresenter : IDisposable
    {
        const string WORLD_SCENE_NAME = "WorldScene";
        const string TITLE_SCENE_NAME = "TitleScene";

        readonly SceneLoader sceneLoader = null;
        readonly ScreenTransition screenTransition = null;
        readonly GameOverView view = null;
        readonly CompositeDisposable disposables = new();

        [Inject]
        public GameOverPresenter(
            SceneLoader sceneLoader,
            ScreenTransition screenTransition,
            GameOverView view)
        {
            this.sceneLoader = sceneLoader;
            this.screenTransition = screenTransition;
            this.view = view;

            view.OnClickContinueObservable()
                .First()
                .Subscribe(_ => ChangeSceneAsync(WORLD_SCENE_NAME).Forget())
                .AddTo(this.disposables);

            view.OnClickReturnTitleObservable()
                .First()
                .Subscribe(_ => ChangeSceneAsync(TITLE_SCENE_NAME).Forget())
                .AddTo(this.disposables);
        }

        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
        }

        async UniTask ChangeSceneAsync(string sceneName)
        {
            await this.screenTransition.FadeOut();
            await this.sceneLoader.LoadAsync(sceneName);
        }
    }
}