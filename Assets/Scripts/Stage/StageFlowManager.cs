using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Rail
{
    /// <summary>
    /// ステージ中の状態を監視し、ステージの生成、エリア切り替えやプレイヤーのスポーンなどを取りまとめるクラス。
    /// </summary>
    public class StageFlowManager : MonoBehaviour
    {
        [SerializeField]
        StageDefinition stageDefinition = null;

        StageArea currentArea = null;
        bool isChangingArea = false;

        PlayerManager playerManager = null;
        ScreenTransition screenTransition = null;
        UniTaskCompletionSource respawnFadeOutCompletion = null;
        bool isRespawning = false;
        bool hasPlayedInitialFadeIn = false;

        void Start()
        {
            this.playerManager = PlayerManager.Instance;
            this.screenTransition = ScreenTransition.Instance;

            if (this.playerManager == null)
            {
                Debug.LogWarning("[StageFlowManager]PlayerManager not found.");
                return;
            }

            // ScreenTransitionは演出専用。無くてもステージ進行(ゴール判定など)は止めない
            if (this.screenTransition == null)
            {
                Debug.LogWarning("[StageFlowManager]ScreenTransition not found.");
            }

            this.playerManager.OnPlayerSpawnedObservable()
                .Subscribe(_ => PlayInitialFadeIn().Forget())
                .AddTo(this);

            this.playerManager.OnPlayerDeadObservable()
                .Subscribe(_ => StartRespawnFadeOut())
                .AddTo(this);

            this.playerManager.OnPlayerDiedObservable()
                .Subscribe(_ => OnPlayerDiedAsync().Forget())
                .AddTo(this);

            if (this.playerManager.CurrentPlayer != null)
            {
                PlayInitialFadeIn().Forget();
            }

            if (this.stageDefinition == null)
            {
                Debug.LogWarning("[StageFlowManager] StageDefinition not found.");
                return;
            }

            this.currentArea = this.stageDefinition.InitialArea;

            this.stageDefinition.OnAreaEventObservable()
                .Subscribe(HandleAreaEvent)
                .AddTo(this);
        }

        async UniTaskVoid PlayInitialFadeIn()
        {
            if (this.hasPlayedInitialFadeIn) return;

            this.hasPlayedInitialFadeIn = true;

            var transition = ScreenTransition.Instance;
            if (transition == null)
            {
                Debug.LogWarning("[StageFlowManager]ScreenTransition not found.");
                return;
            }

            await transition.FadeIn();
        }

        void StartRespawnFadeOut()
        {
            if (this.isRespawning) return;

            this.isRespawning = true;
            this.respawnFadeOutCompletion = new UniTaskCompletionSource();
            FadeOutForRespawnAsync().Forget();
        }

        async UniTaskVoid FadeOutForRespawnAsync()
        {
            var transition = ScreenTransition.Instance;
            if (transition == null)
            {
                Debug.LogWarning("[StageFlowManager]ScreenTransition not found.");
                this.respawnFadeOutCompletion?.TrySetResult();
                return;
            }

            await transition.FadeOut();
            this.respawnFadeOutCompletion?.TrySetResult();
        }

        void HandleAreaEvent(AreaEventInfo info)
        {
            switch (info)
            {
                case AreaTransitionInfo areaTransition:
                    ChangeAreaAsync(areaTransition).Forget();
                    break;
                case CheckPointInfo checkPoint:
                    HandleCheckPoint(checkPoint);
                    break;
                case GoalInfo goal:
                    HandleGoal(goal);
                    break;
                case StageExitInfo stageExit:
                    HandleStageExit(stageExit);
                    break;
                default:
                    Debug.LogWarning(
                        $"Unsupported AreaEventInfo: "
                        + $"{info.GetType().Name}");
                    break;
            }
        }

        async UniTaskVoid ChangeAreaAsync(AreaTransitionInfo info)
        {
            if (this.isChangingArea) return;

            if (info.DestinationArea == null
                || info.DestinationPoint == null)
            {
                Debug.LogWarning(
                    "[StageFlowManager]"
                    + "Area transition destination is invalid.");
                return;
            }

            // 暗転開始
            this.isChangingArea = true;
            if (this.screenTransition != null)
            {
                await this.screenTransition.FadeOut();
            }

            // 前のエリアを保持
            var previousArea = this.currentArea;

            // 新しいエリアをアクティブ化
            info.DestinationArea.SetAreaActive(true);

            // プレイヤーを遷移先に移動
            this.playerManager.MoveTo(info.DestinationPoint);

            // 元のエリアを非アクティブ化
            if (previousArea != null
                && previousArea != info.DestinationArea)
            {
                previousArea.SetAreaActive(false);
            }

            // 遷移先を保持
            this.currentArea = info.DestinationArea;

            // 暗転を解除
            if (this.screenTransition != null)
            {
                await this.screenTransition.FadeIn();
            }
            this.isChangingArea = false;
        }

        void HandleCheckPoint(CheckPointInfo info)
        {
            if (info.RespawnPoint == null) return;

            this.playerManager.SetRespawnPoint(info.RespawnPoint);

            Debug.Log($"Checkpoint: {info.CheckPointId}");
        }

        void HandleGoal(GoalInfo info)
        {
            switch (info.GoalType)
            {
                case GoalType.Normal:
                    break;
                case GoalType.Secret:
                    break;
                case GoalType.Fake:
                    break;
            }

            Debug.Log($"Goal: {info.GoalType}");
        }

        void HandleStageExit(StageExitInfo info)
        {
            Debug.Log("Exit stage.");
        }

        async UniTaskVoid OnPlayerDiedAsync()
        {
            if (this.isRespawning == false)
            {
                StartRespawnFadeOut();
            }

            if (this.respawnFadeOutCompletion != null)
            {
                await this.respawnFadeOutCompletion.Task;
            }

            this.playerManager.TryRespawn();

            var transition = ScreenTransition.Instance;
            if (transition != null)
            {
                await transition.FadeIn();
            }

            this.respawnFadeOutCompletion = null;
            this.isRespawning = false;
        }
    }
}
