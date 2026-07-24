using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Rail
{
    /// <summary>
    /// AudioMixer上の出力系統。
    /// </summary>
    public enum AudioBus
    {
        Master,
        BGM,
        SE,
        UI
    }

    /// <summary>
    /// BGM、SE、UI音の再生とAudioMixerの音量を一元管理する。
    /// Sceneをまたいで常駐し、具体的なAudioResourceは利用側から受け取る。
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        // 再生APIの既定値。
        const float DEFAULT_FADE_DURATION = 0.5f;
        const float DEFAULT_VOLUME_SCALE = 1.0f;

        // 振幅とデシベルを相互変換するための定数。
        const float MIN_DECIBEL = -80.0f;
        const float AMPLITUDE_DECIBEL_FACTOR = 20.0f;
        const float DECIBEL_LOGARITHM_BASE = 10.0f;

        // AudioMixer側でExposeしておくパラメーター名。
        const string MASTER_VOLUME_PARAMETER = "MasterVolume";
        const string BGM_VOLUME_PARAMETER = "BGMVolume";
        const string SE_VOLUME_PARAMETER = "SEVolume";
        const string UI_VOLUME_PARAMETER = "UIVolume";

        [Header("Mixer")]
        [SerializeField]
        AudioMixer audioMixer = null;

        [SerializeField]
        AudioMixerGroup bgmMixerGroup = null;

        [SerializeField]
        AudioMixerGroup seMixerGroup = null;

        [SerializeField]
        AudioMixerGroup uiMixerGroup = null;

        [Header("Sources")]
        [SerializeField]
        AudioSource bgmSource = null;

        [SerializeField, Min(1)]
        int seSourcePoolSize = 8;

        [Header("3D SE")]
        [SerializeField, Min(0.0f)]
        float seMinDistance = 1.0f;

        [SerializeField, Min(0.0f)]
        float seMaxDistance = 30.0f;

        // SEとUI音を同時再生するためのAudioSourceプール。
        readonly List<AudioSource> seSources = new();

        // 新しいBGM操作が来たとき、実行中のフェードを中断するために使用する。
        CancellationTokenSource bgmTransitionCancellation = null;
        int nextSeSourceIndex = 0;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DontDestroyOnLoad(this.gameObject);

            SetupBGMSource();
            SetupSESources();
        }

        void OnDestroy()
        {
            CancelBGMTransition();
        }

        /// <summary>
        /// BGMを再生する。
        /// 別のBGMが再生中ならフェードアウト後に切り替え、フェードインする。
        /// </summary>
        /// <param name="resource">再生するAudioClipまたはAudio Random Container。</param>
        /// <param name="fadeDuration">フェードアウト・フェードインそれぞれの時間。</param>
        public void PlayBGM(
            AudioResource resource,
            float fadeDuration = DEFAULT_FADE_DURATION)
        {
            if (resource == null)
            {
                Debug.LogWarning("[AudioManager] BGM resource is null.", this);
                return;
            }

            var cancellationToken = CreateBGMTransitionCancellation();
            PlayBGMAsync(
                resource,
                Mathf.Max(0.0f, fadeDuration),
                cancellationToken).Forget();
        }

        /// <summary>
        /// 現在のBGMをフェードアウトして停止する。
        /// </summary>
        /// <param name="fadeDuration">停止までのフェード時間。</param>
        public void StopBGM(
            float fadeDuration = DEFAULT_FADE_DURATION)
        {
            var cancellationToken = CreateBGMTransitionCancellation();
            StopBGMAsync(
                Mathf.Max(0.0f, fadeDuration),
                cancellationToken).Forget();
        }

        /// <summary>
        /// 位置に影響されない2D効果音を再生する。
        /// </summary>
        /// <param name="resource">再生するAudioResource。</param>
        /// <param name="volumeScale">今回の再生にだけ適用する音量倍率。</param>
        public void PlaySE(
            AudioResource resource,
            float volumeScale = DEFAULT_VOLUME_SCALE)
        {
            PlayOneShot(
                resource,
                this.seMixerGroup,
                Vector3.zero,
                false,
                volumeScale);
        }

        /// <summary>
        /// 指定したワールド座標から3D効果音を再生する。
        /// </summary>
        /// <param name="resource">再生するAudioResource。</param>
        /// <param name="position">音を発生させるワールド座標。</param>
        /// <param name="volumeScale">今回の再生にだけ適用する音量倍率。</param>
        public void PlaySEAt(
            AudioResource resource,
            Vector3 position,
            float volumeScale = DEFAULT_VOLUME_SCALE)
        {
            PlayOneShot(
                resource,
                this.seMixerGroup,
                position,
                true,
                volumeScale);
        }

        /// <summary>
        /// 位置に影響されないUI効果音を再生する。
        /// </summary>
        /// <param name="resource">再生するAudioResource。</param>
        /// <param name="volumeScale">今回の再生にだけ適用する音量倍率。</param>
        public void PlayUI(
            AudioResource resource,
            float volumeScale = DEFAULT_VOLUME_SCALE)
        {
            PlayOneShot(
                resource,
                this.uiMixerGroup,
                Vector3.zero,
                false,
                volumeScale);
        }

        /// <summary>
        /// SE用プールで再生している音をすべて停止する。
        /// UI音も同じプールを使用しているため、一緒に停止する。
        /// </summary>
        public void StopAllSE()
        {
            foreach (var source in this.seSources)
            {
                source.Stop();
                source.resource = null;
            }
        }

        /// <summary>
        /// Mixerの音量を設定する。
        /// Linear値(0.0f～1.0f)をデシベルへ変換して設定する。
        /// </summary>
        /// <param name="bus">音量を変更するMixerの系統。</param>
        /// <param name="linearVolume">0.0fが無音、1.0fが元の音量。</param>
        public void SetVolume(AudioBus bus, float linearVolume)
        {
            if (this.audioMixer == null)
            {
                Debug.LogWarning("[AudioManager] AudioMixer is not assigned.", this);
                return;
            }

            var parameterName = GetVolumeParameterName(bus);
            var decibel = LinearToDecibel(linearVolume);

            if (!this.audioMixer.SetFloat(parameterName, decibel))
            {
                Debug.LogWarning(
                    $"[AudioManager] Mixer parameter is not exposed: {parameterName}",
                    this);
            }
        }

        /// <summary>
        /// Mixerの音量をLinear値で取得する。
        /// </summary>
        /// <param name="bus">取得するMixerの系統。</param>
        /// <returns>0.0f～1.0fへ変換した音量。</returns>
        public float GetVolume(AudioBus bus)
        {
            if (this.audioMixer == null)
            {
                Debug.LogWarning("[AudioManager] AudioMixer is not assigned.", this);
                return DEFAULT_VOLUME_SCALE;
            }

            var parameterName = GetVolumeParameterName(bus);

            if (!this.audioMixer.GetFloat(parameterName, out var decibel))
            {
                Debug.LogWarning(
                    $"[AudioManager] Mixer parameter is not exposed: {parameterName}",
                    this);
                return DEFAULT_VOLUME_SCALE;
            }

            return DecibelToLinear(decibel);
        }

        /// <summary>
        /// BGM用AudioSourceを準備する。
        /// Inspectorで未設定の場合は子GameObjectとして自動生成する。
        /// </summary>
        void SetupBGMSource()
        {
            if (this.bgmSource == null)
            {
                var sourceObject = new GameObject("BGM Audio Source");
                sourceObject.transform.SetParent(this.transform, false);
                this.bgmSource = sourceObject.AddComponent<AudioSource>();
            }

            ConfigureSource(
                this.bgmSource,
                this.bgmMixerGroup,
                false);
        }

        /// <summary>
        /// SEとUI音を重ねて鳴らすためのAudioSourceプールを生成する。
        /// </summary>
        void SetupSESources()
        {
            this.seSources.Clear();
            var poolSize = Mathf.Max(1, this.seSourcePoolSize);

            for (var i = 0; i < poolSize; i++)
            {
                var sourceObject = new GameObject($"SE Audio Source {i + 1}");
                sourceObject.transform.SetParent(this.transform, false);

                var source = sourceObject.AddComponent<AudioSource>();
                ConfigureSource(source, this.seMixerGroup, false);
                this.seSources.Add(source);
            }
        }

        /// <summary>
        /// 再利用するAudioSourceへ共通設定を適用する。
        /// 直前の再生が2Dでも3Dでも設定が残らないよう、毎回すべて上書きする。
        /// </summary>
        void ConfigureSource(
            AudioSource source,
            AudioMixerGroup mixerGroup,
            bool is3D)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.volume = DEFAULT_VOLUME_SCALE;
            source.pitch = DEFAULT_VOLUME_SCALE;
            source.spatialBlend = is3D ? 1.0f : 0.0f;
            source.outputAudioMixerGroup = mixerGroup;
            source.minDistance = this.seMinDistance;
            source.maxDistance = Mathf.Max(
                this.seMinDistance,
                this.seMaxDistance);
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        /// <summary>
        /// プールからAudioSourceを取得し、単発のAudioResourceを再生する。
        /// </summary>
        void PlayOneShot(
            AudioResource resource,
            AudioMixerGroup mixerGroup,
            Vector3 position,
            bool is3D,
            float volumeScale)
        {
            if (resource == null)
            {
                Debug.LogWarning("[AudioManager] Audio resource is null.", this);
                return;
            }

            var source = GetAvailableSESource();
            ConfigureSource(source, mixerGroup, is3D);

            source.transform.position = is3D
                ? position
                : this.transform.position;
            source.resource = resource;
            source.volume = Mathf.Clamp01(volumeScale);
            source.Play();
        }

        /// <summary>
        /// 停止中のAudioSourceを優先して取得する。
        /// すべて再生中なら、ラウンドロビンで次のAudioSourceを再利用する。
        /// </summary>
        AudioSource GetAvailableSESource()
        {
            for (var offset = 0; offset < this.seSources.Count; offset++)
            {
                var index =
                    (this.nextSeSourceIndex + offset) %
                    this.seSources.Count;
                var source = this.seSources[index];

                if (source.isPlaying) continue;

                this.nextSeSourceIndex =
                    (index + 1) % this.seSources.Count;
                return source;
            }

            // 全ソースが再生中なので、次の候補を停止して新しい音へ割り当てる。
            var reusedSource =
                this.seSources[this.nextSeSourceIndex];
            this.nextSeSourceIndex =
                (this.nextSeSourceIndex + 1) %
                this.seSources.Count;

            reusedSource.Stop();
            return reusedSource;
        }

        /// <summary>
        /// 現在のBGMを必要に応じてフェードアウトし、新しいBGMへ切り替える。
        /// </summary>
        async UniTaskVoid PlayBGMAsync(
            AudioResource resource,
            float fadeDuration,
            CancellationToken cancellationToken)
        {
            try
            {
                if (this.bgmSource.resource == resource &&
                    this.bgmSource.isPlaying)
                {
                    await FadeBGMAsync(
                        DEFAULT_VOLUME_SCALE,
                        fadeDuration,
                        cancellationToken);
                    return;
                }

                if (this.bgmSource.isPlaying)
                {
                    await FadeBGMAsync(
                        0.0f,
                        fadeDuration,
                        cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                this.bgmSource.Stop();
                this.bgmSource.resource = resource;

                // AudioClipはAudioSource側でループする。
                // Audio Random ContainerはContainer側のループ設定に従う。
                this.bgmSource.loop = resource is AudioClip;
                this.bgmSource.volume = fadeDuration > 0.0f
                    ? 0.0f
                    : DEFAULT_VOLUME_SCALE;
                this.bgmSource.Play();

                await FadeBGMAsync(
                    DEFAULT_VOLUME_SCALE,
                    fadeDuration,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 新しいBGM操作によってキャンセルされた。
            }
        }

        /// <summary>
        /// 現在のBGMをフェードアウトして停止し、AudioResource参照を解放する。
        /// </summary>
        async UniTaskVoid StopBGMAsync(
            float fadeDuration,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!this.bgmSource.isPlaying)
                {
                    this.bgmSource.resource = null;
                    this.bgmSource.volume = DEFAULT_VOLUME_SCALE;
                    return;
                }

                await FadeBGMAsync(
                    0.0f,
                    fadeDuration,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                this.bgmSource.Stop();
                this.bgmSource.resource = null;
                this.bgmSource.volume = DEFAULT_VOLUME_SCALE;
            }
            catch (OperationCanceledException)
            {
                // 新しいBGM操作によってキャンセルされた。
            }
        }

        /// <summary>
        /// BGM用AudioSourceの音量を指定値まで補間する。
        /// ポーズ中もフェードするよう、Time.unscaledDeltaTimeを使用する。
        /// </summary>
        async UniTask FadeBGMAsync(
            float targetVolume,
            float duration,
            CancellationToken cancellationToken)
        {
            if (duration <= 0.0f)
            {
                this.bgmSource.volume = targetVolume;
                return;
            }

            var startVolume = this.bgmSource.volume;
            var elapsed = 0.0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                this.bgmSource.volume = Mathf.Lerp(
                    startVolume,
                    targetVolume,
                    progress);

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken);
            }

            this.bgmSource.volume = targetVolume;
        }

        /// <summary>
        /// 以前のBGM操作をキャンセルし、今回の操作専用Tokenを生成する。
        /// </summary>
        CancellationToken CreateBGMTransitionCancellation()
        {
            CancelBGMTransition();

            this.bgmTransitionCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    this.GetCancellationTokenOnDestroy());

            return this.bgmTransitionCancellation.Token;
        }

        /// <summary>
        /// 実行中のBGMフェードをキャンセルして破棄する。
        /// </summary>
        void CancelBGMTransition()
        {
            this.bgmTransitionCancellation?.Cancel();
            this.bgmTransitionCancellation?.Dispose();
            this.bgmTransitionCancellation = null;
        }

        /// <summary>
        /// AudioBusに対応するAudioMixerのExposeパラメーター名を返す。
        /// </summary>
        static string GetVolumeParameterName(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Master => MASTER_VOLUME_PARAMETER,
                AudioBus.BGM => BGM_VOLUME_PARAMETER,
                AudioBus.SE => SE_VOLUME_PARAMETER,
                AudioBus.UI => UI_VOLUME_PARAMETER,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(bus),
                    bus,
                    null)
            };
        }

        /// <summary>
        /// Linear値(0.0f～1.0f)をデシベル値に変換する。
        /// 振幅比なので「20 × log10(Linear)」を使用する。
        /// </summary>
        static float LinearToDecibel(float linearVolume)
        {
            linearVolume = Mathf.Clamp01(linearVolume);

            if (linearVolume <= 0.0f)
            {
                return MIN_DECIBEL;
            }

            var decibel =
                Mathf.Log10(linearVolume) *
                AMPLITUDE_DECIBEL_FACTOR;

            return Mathf.Max(decibel, MIN_DECIBEL);
        }

        /// <summary>
        /// デシベル値をLinear値(0.0f～1.0f)へ変換する。
        /// LinearToDecibelの逆算で「10 ^ (dB / 20)」を使用する。
        /// </summary>
        static float DecibelToLinear(float decibel)
        {
            // 無音として使用しているMIN_DECIBELは計算上は0.0001になるため、直接0を返す。
            if (decibel <= MIN_DECIBEL)
            {
                return 0.0f;
            }

            var linearVolume = Mathf.Pow(
                DECIBEL_LOGARITHM_BASE,
                decibel / AMPLITUDE_DECIBEL_FACTOR);

            return Mathf.Clamp01(linearVolume);
        }
    }
}
