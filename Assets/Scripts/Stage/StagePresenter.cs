using System;
using System.Threading;
using VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;


namespace Rail
{
    public class StagePresenter : IDisposable
    {
        readonly StageLoader stageLoader = null;
        readonly StageModel stageModel = null;
        readonly StageEventBus stageEventBus = null;
        readonly IPlayerEventBus playerEventBus = null;
        readonly ScreenTransition screenTransition = null;
        readonly CompositeDisposable disposables = new();

        StageDefinition stageDefinition = null;

        [Inject]
        public StagePresenter(
            StageRequest stageRequest,
            StageLoader stageLoader,
            StageModel stageModel,
            StageEventBus stageEventBus,
            IPlayerEventBus playerEventBus,
            ScreenTransition screenTransition)
        {
            this.stageLoader = stageLoader;
            this.stageModel = stageModel;
            this.stageEventBus = stageEventBus;
            this.playerEventBus = playerEventBus;
            this.screenTransition = screenTransition;
            
            // エリア内Trigger
            this.stageEventBus.OnAreaTriggerObservable()
                .Subscribe(HandleAreaEvent)
                .AddTo(this.disposables);

            // 残機減った通知
            this.playerEventBus.OnLifeConsumedObservable()
                .Subscribe(_ =>
                {
                    this.stageDefinition.View.ResetEnemies();
                    this.stageEventBus.PublishRespawnRequest(this.stageModel.Entity.SpawnPoint);
                })
                .AddTo(this.disposables);

            // 選択したステージを読み込み
            LoadStageAsync(stageRequest).Forget();
        }

        void IDisposable.Dispose()
        {
            this.disposables.Dispose();
        }

        async UniTask LoadStageAsync(StageRequest request)
        {
            // ステージのシーンをAdditiveロードする。
            // ステージの構成情報を取得
            this.stageDefinition = await this.stageLoader.LoadStageAsync(
                request.WorldId,
                request.StageId);
            
            if (this.stageDefinition == null)
            {
                Debug.LogError("[StagePresenter] Stage definition is null.");
                return;
            }

            this.stageDefinition.View.ResetEnemies();

            // 初期エリアのセットアップ
            ChangeArea(this.stageDefinition.InitialArea.AreaId, this.stageDefinition.InitialArea.StartPoint);
        }

        void HandleAreaEvent(AreaEventInfo eventInfo)
        {
            if (eventInfo is not AreaTransitionInfo transitionInfo)
            {
                return;
            }

            this.ChangeArea(transitionInfo.DestinationArea.AreaId, transitionInfo.DestinationPoint);
        }

        void ChangeArea(string destinationAreaId, SplineAnchor destinationPoint)
        {
            // エリア切り替え処理
            if (this.stageDefinition == null)
            {
                Debug.LogError("[StagePresenter] Stage is not loaded.");
                return;
            }

            var view = this.stageDefinition.View;

            // 書き換える前に、今のエリアを控えておく（初回はnull）
            var previousAreaId= this.stageModel.Entity?.AreaId;

            if (!this.stageModel.TryChangeArea(destinationAreaId, destinationPoint))
            {
                return;
            }

            // 遷移先を表示して、プレイヤーを移動する。
            view.SetAreaActive(destinationAreaId, true);

            // 準備完了を通知
            this.stageEventBus.PublishPrepared(destinationPoint);
            
            // プレイヤー移動後に以前のエリアを非表示にする。
            if (previousAreaId != null)
            {
                view.SetAreaActive(previousAreaId, false);
            }
        }
    }
}
