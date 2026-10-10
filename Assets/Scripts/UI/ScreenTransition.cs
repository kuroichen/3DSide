using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Rail
{    
    public class ScreenTransition : MonoBehaviour
    {
        [SerializeField]
        Image screen = null;

        [SerializeField]
        float defaultDuration = 0.5f;

        [SerializeField]
        bool startBlack = false;

        protected void Awake()
        {
            SetAlpha(this.startBlack ? 1.0f : 0.0f);
            SetRaycastTarget(this.startBlack);
        }

        /// <summary>
        /// フェードを行わず、即座に暗転状態へ切り替える。
        /// Stage開始時にFadeInの始点を揃えるために使用する。
        /// </summary>
        public void ShowBlackImmediately()
        {
            SetAlpha(1.0f);
            SetRaycastTarget(true);
        }

        /// <summary>
        /// 暗転解除
        /// </summary>
        public async UniTask FadeIn(float duration = -1.0f)
        {
            await FadeTo(0.0f, duration);
            SetRaycastTarget(false);
        }

        public async UniTask FadeOut(float duration = -1.0f)
        {
            SetRaycastTarget(true);
            await FadeTo(1.0f, duration);
        }

        public async UniTask FadeOutIn(float duration = -1.0f)
        {
            await FadeOut(duration);
            await FadeIn(duration);
        }

        async UniTask FadeTo(float targetAlpha, float duration)
        {
            if (this.screen == null)
            {
                Debug.LogWarning("[ScreenTransition]Screen image is not assigned.");
                return;
            }

            var fadeDuration = duration >= 0.0f
                ? duration
                : this.defaultDuration;

            if (fadeDuration <= 0.0f)
            {
                SetAlpha(targetAlpha);
                return;
            }

            var startAlpha = this.screen.color.a;
            var elapsed = 0.0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / fadeDuration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            }

            SetAlpha(targetAlpha);
        }

        void SetAlpha(float alpha)
        {
            if (this.screen == null) return;

            var color = this.screen.color;
            color.a = alpha;
            this.screen.color = color;
        }

        void SetRaycastTarget(bool enable)
        {
            if (this.screen == null) return;

            this.screen.raycastTarget = enable;
        }
    }
}
