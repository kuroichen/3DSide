using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class StageFlowManager : MonoBehaviour
    {
        PlayerManager playerManager = null;
        UniTaskCompletionSource respawnFadeOutCompletion = null;
        bool isRespawning = false;
        bool hasPlayedInitialFadeIn = false;

        void Start()
        {
            this.playerManager = PlayerManager.Instance;
            if (this.playerManager == null)
            {
                Debug.LogWarning("[StageFlowManager]PlayerManager not found.");
                return;
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
