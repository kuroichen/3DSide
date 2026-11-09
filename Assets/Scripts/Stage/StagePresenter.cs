using System;
using System.Threading;
using VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using System.ComponentModel;


namespace Rail
{
    public class StagePresenter : IDisposable
    {
        const float GOAL_DISPLAY_SECONDS = 3.0f;
        const string WORLD_SCENE_NAME = "WorldScene";

        readonly SceneLoader sceneLoader = null;
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
            ScreenTransition screenTransition,
            SceneLoader sceneLoader)
        {
            this.stageLoader = stageLoader;
            this.stageModel = stageModel;
            this.stageEventBus = stageEventBus;
            this.playerEventBus = playerEventBus;
            this.screenTransition = screenTransition;
            this.sceneLoader = sceneLoader;
            
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
            
            // 残機使い切った通知
            this.playerEventBus.OnLivesDepletedObservable()
                .Subscribe(_ => GameOverAsync().Forget())
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

            // プレイフェーズに移行
            this.stageModel.TryChangePhase(StagePhase.Playing);
        }

        void HandleAreaEvent(AreaEventInfo eventInfo)
        {
            // プレイ中以外（ロード中・ゴール後・退出中）はエリアイベントを受け付けない。
            if (this.stageModel.Phase != StagePhase.Playing) return;

            switch (eventInfo.EventType)
            {
                case AreaEventType.Transition:
                    {
                        if (eventInfo is AreaTransitionInfo info)
                        {
                            this.ChangeArea(info.DestinationArea.AreaId, info.DestinationPoint);
                        }
                    }
                    break;
                case AreaEventType.Goal:
                    {
                        if (eventInfo is GoalInfo info)
                        {
                            ClearStageAsync(info.GoalType).Forget();
                        }
                    }
                    break;
                case AreaEventType.StageExit:
                    {
                        if (eventInfo is StageExitInfo)
                        {
                            ExitStageAsync().Forget();
                        }
                    }
                    break;
                default:
                    Debug.LogError($"[StagePresenter][HandleAreaEvent] invalid enum argument.");
                    break;
            }    
        }

        /// <summary>
        /// ゲームオーバー処理
        /// </summary>
        async UniTask GameOverAsync()
        {
            if (!this.stageModel.TryChangePhase(StagePhase.GameOver)) return;

            this.stageEventBus.PublishGameOver();
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

        async UniTask ChangeSceneAsync(string sceneName)
        {
            await this.screenTransition.FadeOut();
            await this.sceneLoader.LoadAsync(sceneName);
        }

        
        async UniTask ClearStageAsync(GoalType goalType)
        {
            if (!this.stageModel.TryChangePhase(StagePhase.Cleared)) return;

            this.stageEventBus.PublishStageCleared(goalType);

            await UniTask.Delay(TimeSpan.FromSeconds(GOAL_DISPLAY_SECONDS));

            await ChangeSceneAsync(WORLD_SCENE_NAME);
        }

        async UniTask ExitStageAsync()
        {
            if (!this.stageModel.TryChangePhase(StagePhase.Exiting)) return;

            await ChangeSceneAsync(WORLD_SCENE_NAME);
        }
    }
}
