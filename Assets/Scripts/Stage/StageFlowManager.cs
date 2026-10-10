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
        PlayerManager playerManager = null;
        ScreenTransition screenTransition = null;

        UniTaskCompletionSource respawnFadeOutCompletion = null;
        
        bool isRespawning = false;
        bool hasPlayedInitialFadeIn = false;

        void Start()
        {
            this.playerManager = PlayerManager.Instance;
            this.screenTransition = null;

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
            else
            {
                this.screenTransition.ShowBlackImmediately();
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
        }

        async UniTaskVoid PlayInitialFadeIn()
        {
            if (this.hasPlayedInitialFadeIn) return;

            this.hasPlayedInitialFadeIn = true;

            if (this.screenTransition == null) return;

            await this.screenTransition.FadeIn();
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
            if (this.screenTransition != null)
            {
                await this.screenTransition.FadeOut();
            }

            this.respawnFadeOutCompletion?.TrySetResult();
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

            if (this.screenTransition != null)
            {
                await this.screenTransition.FadeIn();
            }

            this.respawnFadeOutCompletion = null;
            this.isRespawning = false;
        }
    }
}
