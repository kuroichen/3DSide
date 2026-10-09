using UnityEngine;
using UnityEngine.Audio;

namespace Rail
{
    /// <summary>
    /// BootstrapがAudioManagerへ渡すAudioMixer設定。
    /// Resourcesから読み込むことで、どのSceneから起動しても同じMixerを使用する。
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioMixerSettings",
        menuName = "Rail/Audio/Audio Mixer Settings")]
    public sealed class AudioMixerSettings : ScriptableObject
    {
        [SerializeField]
        AudioMixer audioMixer = null;

        [SerializeField]
        AudioMixerGroup masterMixerGroup = null;

        [SerializeField]
        AudioMixerGroup bgmMixerGroup = null;

        [SerializeField]
        AudioMixerGroup seMixerGroup = null;

        [SerializeField]
        AudioMixerGroup uiMixerGroup = null;

        public AudioMixer AudioMixer => this.audioMixer;
        public AudioMixerGroup MasterMixerGroup => this.masterMixerGroup;
        public AudioMixerGroup BGMMixerGroup => this.bgmMixerGroup;
        public AudioMixerGroup SEMixerGroup => this.seMixerGroup;
        public AudioMixerGroup UIMixerGroup => this.uiMixerGroup;

        /// <summary>
        /// Mixerと必要な出力Groupがすべて設定されているかを返す。
        /// </summary>
        public bool IsValid =>
            this.audioMixer != null
            && this.masterMixerGroup != null
            && this.bgmMixerGroup != null
            && this.seMixerGroup != null
            && this.uiMixerGroup != null;
    }
}
