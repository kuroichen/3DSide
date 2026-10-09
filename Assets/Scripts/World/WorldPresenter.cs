using UnityEngine;
using VContainer;
using VContainer.Unity;
using UniRx;
using Cysharp.Threading.Tasks;
using System;

namespace Rail
{
    public class WorldPresenter : IDisposable
    {
        SceneLoader sceneLoader = null;
        WorldLoader worldLoader = null;
        WorldView worldView = null;

        CompositeDisposable disposables = new();

        [Inject]
        public WorldPresenter(
            WorldLoader worldLoader,
            WorldView worldView,
            SceneLoader sceneLoader)
        {
            this.worldLoader = worldLoader;
            this.worldView = worldView;
            this.sceneLoader = sceneLoader;

            this.worldView.OnClickAsObservable()
                .Select(stageId => new StageRequest(WorldId.World1, stageId))
                .Subscribe(request => this.OpenStageAsync(request).Forget())
                .AddTo(this.disposables);
        }
        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
        }

        private async UniTask OpenStageAsync(StageRequest request)
        {
            using (LifetimeScope.Enqueue(
                builder =>
                {
                    builder.RegisterInstance<StageRequest>(request);
                }))
            {
                await this.sceneLoader.LoadAsync("StageScene");
            }
        }
    }
}
