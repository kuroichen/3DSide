using UnityEngine;
using UnityEngine.UI;

namespace Rail
{
    /// <summary>
    /// ゲーム全体で使用するScene非依存サービスを起動する。
    /// RuntimeInitializeOnLoadMethodを使用するため、どのSceneからPlayしても自動的に実行される。
    /// </summary>
    public static class GameBootstrap
    {
        const string BOOTSTRAP_OBJECT_NAME = "[Game Bootstrap]";
        const string SCREEN_OBJECT_NAME = "Screen";
        const string AUDIO_MIXER_SETTINGS_PATH =
            "Audio/AudioMixerSettings";
        const int TRANSITION_SORTING_ORDER = 1000;

        /// <summary>
        /// 最初のSceneが読み込まれる前に、常駐オブジェクトを生成する。
        /// 専用のBootstrap Sceneを経由しないため、編集中のStage Sceneから直接Playできる。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var root = new GameObject(
                BOOTSTRAP_OBJECT_NAME,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Object.DontDestroyOnLoad(root);

            SetupCanvas(root);
            CreateAudioManager(root);
            CreateScreenTransition(root);
        }

        static void SetupCanvas(GameObject root)
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = TRANSITION_SORTING_ORDER;
        }

        static void CreateAudioManager(GameObject root)
        {
            var audioManager = root.AddComponent<AudioManager>();
            var settings = Resources.Load<AudioMixerSettings>(
                AUDIO_MIXER_SETTINGS_PATH);

            audioManager.Initialize(settings);
        }

        static void CreateScreenTransition(GameObject root)
        {
            var screenObject = new GameObject(
                SCREEN_OBJECT_NAME,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            screenObject.transform.SetParent(root.transform, false);

            var screen = screenObject.GetComponent<Image>();
            screen.color = Color.black;

            var rectTransform = screen.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var transition = root.AddComponent<ScreenTransition>();

            // Bootstrap自体はどのSceneでも起動するので、通常時は透明にしておく。
            // Stage開始時の暗転はStageFlowManagerが明示的に設定する。
            transition.Initialize(screen, false);
        }
    }
}
