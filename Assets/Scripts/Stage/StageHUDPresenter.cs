
using System;
using UniRx;

namespace Rail
{
    public sealed class StageHUDPresenter : IDisposable
    {
        CompositeDisposable disposables = new();

        public StageHUDPresenter(
            StageHUDView hudView,
            PlayerModel playerModel,
            IStageEventBus stageEventBus)
        {
            playerModel.OnHpChangedObservable()
                .StartWith(playerModel.Entity.Hp)
                .Subscribe(hudView.SetHitPoint)
                .AddTo(this.disposables);
            
            playerModel.OnLivesChangedObservable()
                .StartWith(playerModel.Entity.Lives)
                .Subscribe(hudView.SetLives)
                .AddTo(this.disposables);

            stageEventBus.OnStageClearedObservable()
                .Subscribe(_ => hudView.ShowGoal())
                .AddTo(this.disposables);
        }

        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
        }
    }
}