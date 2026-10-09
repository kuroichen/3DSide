using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Rail.EditorTools
{
    /// <summary>
    /// ゲーム共通AudioMixerとBootstrap用設定アセットを生成する。
    /// UnityのAudioMixer編集APIは内部APIのため、必要な操作だけReflectionで呼び出す。
    /// </summary>
    public static class AudioMixerSetupTool
    {
        const string AUDIO_FOLDER_PATH = "Assets/Audio";
        const string RESOURCES_FOLDER_PATH = "Assets/Resources";
        const string RESOURCES_AUDIO_FOLDER_PATH =
            "Assets/Resources/Audio";
        const string MIXER_PATH =
            AUDIO_FOLDER_PATH + "/GameAudioMixer.mixer";
        const string SETTINGS_PATH =
            RESOURCES_AUDIO_FOLDER_PATH
            + "/AudioMixerSettings.asset";

        const string MASTER_GROUP_NAME = "Master";
        const string BGM_GROUP_NAME = "BGM";
        const string SE_GROUP_NAME = "SE";
        const string UI_GROUP_NAME = "UI";

        const string MASTER_VOLUME_PARAMETER = "MasterVolume";
        const string BGM_VOLUME_PARAMETER = "BGMVolume";
        const string SE_VOLUME_PARAMETER = "SEVolume";
        const string UI_VOLUME_PARAMETER = "UIVolume";

        const string MIXER_CONTROLLER_TYPE_NAME =
            "UnityEditor.Audio.AudioMixerController";
        const string EXPOSED_PARAMETER_TYPE_NAME =
            "UnityEditor.Audio.ExposedAudioParameter";

        static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        static readonly BindingFlags StaticFlags =
            BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        /// <summary>
        /// 初回導入時は、開いているUnity Editor上でコンパイル完了後に自動生成する。
        /// 生成済みの場合は何もしないため、通常のEditor起動には影響しない。
        /// </summary>
        [InitializeOnLoadMethod]
        static void CreateInitialAssetsWhenMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<AudioMixerSettings>(
                        SETTINGS_PATH) != null)
                {
                    return;
                }

                CreateOrUpdate();
            };
        }

        [MenuItem("Tools/Rail/Audio/Create or Update Audio Mixer")]
        public static void CreateOrUpdate()
        {
            EnsureFolder(AUDIO_FOLDER_PATH);
            EnsureFolder(RESOURCES_FOLDER_PATH);
            EnsureFolder(RESOURCES_AUDIO_FOLDER_PATH);

            var mixer = LoadOrCreateMixer();
            var masterGroup = FindGroup(mixer, MASTER_GROUP_NAME);
            var bgmGroup = FindOrCreateChildGroup(
                mixer,
                masterGroup,
                BGM_GROUP_NAME);
            var seGroup = FindOrCreateChildGroup(
                mixer,
                masterGroup,
                SE_GROUP_NAME);
            var uiGroup = FindOrCreateChildGroup(
                mixer,
                masterGroup,
                UI_GROUP_NAME);

            SetExposedVolumeParameters(
                mixer,
                masterGroup,
                bgmGroup,
                seGroup,
                uiGroup);

            var settings = LoadOrCreateSettings();
            AssignSettings(
                settings,
                mixer,
                masterGroup,
                bgmGroup,
                seGroup,
                uiGroup);

            EditorUtility.SetDirty(mixer);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            Debug.Log(
                "[AudioMixerSetupTool] AudioMixer setup completed.");
        }

        /// <summary>
        /// Unityの-executeMethodから呼び出すための入口。
        /// </summary>
        public static void CreateOrUpdateFromCommandLine()
        {
            CreateOrUpdate();
        }

        static AudioMixer LoadOrCreateMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                MIXER_PATH);
            if (mixer != null)
            {
                return mixer;
            }

            var controllerType = GetEditorType(
                MIXER_CONTROLLER_TYPE_NAME);
            var createMethod = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                StaticFlags);

            if (createMethod == null)
            {
                throw new MissingMethodException(
                    MIXER_CONTROLLER_TYPE_NAME,
                    "CreateMixerControllerAtPath");
            }

            createMethod.Invoke(null, new object[] { MIXER_PATH });
            AssetDatabase.ImportAsset(MIXER_PATH);

            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                MIXER_PATH);

            if (mixer == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create AudioMixer: {MIXER_PATH}");
            }

            return mixer;
        }

        static AudioMixerGroup FindOrCreateChildGroup(
            AudioMixer mixer,
            AudioMixerGroup parent,
            string groupName)
        {
            var existingGroup = mixer.FindMatchingGroups(groupName)
                .FirstOrDefault(group => group.name == groupName);
            if (existingGroup != null)
            {
                return existingGroup;
            }

            var controllerType = mixer.GetType();
            var createGroupMethod = controllerType.GetMethod(
                "CreateNewGroup",
                InstanceFlags);
            var addChildMethod = controllerType.GetMethod(
                "AddChildToParent",
                InstanceFlags);

            if (createGroupMethod == null
                || addChildMethod == null)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "CreateNewGroup / AddChildToParent");
            }

            var group = createGroupMethod.Invoke(
                mixer,
                new object[] { groupName, false });

            addChildMethod.Invoke(
                mixer,
                new object[] { group, parent });

            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();

            return FindGroup(mixer, groupName);
        }

        static AudioMixerGroup FindGroup(
            AudioMixer mixer,
            string groupName)
        {
            var group = mixer.FindMatchingGroups(groupName)
                .FirstOrDefault(candidate =>
                    candidate.name == groupName);

            if (group == null)
            {
                throw new InvalidOperationException(
                    $"AudioMixer group not found: {groupName}");
            }

            return group;
        }

        static void SetExposedVolumeParameters(
            AudioMixer mixer,
            AudioMixerGroup masterGroup,
            AudioMixerGroup bgmGroup,
            AudioMixerGroup seGroup,
            AudioMixerGroup uiGroup)
        {
            var exposedParameterType = GetEditorType(
                EXPOSED_PARAMETER_TYPE_NAME);
            var parameters = Array.CreateInstance(
                exposedParameterType,
                4);

            SetExposedParameter(
                parameters,
                0,
                masterGroup,
                MASTER_VOLUME_PARAMETER,
                exposedParameterType);
            SetExposedParameter(
                parameters,
                1,
                bgmGroup,
                BGM_VOLUME_PARAMETER,
                exposedParameterType);
            SetExposedParameter(
                parameters,
                2,
                seGroup,
                SE_VOLUME_PARAMETER,
                exposedParameterType);
            SetExposedParameter(
                parameters,
                3,
                uiGroup,
                UI_VOLUME_PARAMETER,
                exposedParameterType);

            var property = mixer.GetType().GetProperty(
                "exposedParameters",
                InstanceFlags);
            if (property == null)
            {
                throw new MissingMemberException(
                    mixer.GetType().FullName,
                    "exposedParameters");
            }

            property.SetValue(mixer, parameters);
        }

        static void SetExposedParameter(
            Array parameters,
            int index,
            AudioMixerGroup group,
            string parameterName,
            Type exposedParameterType)
        {
            var groupType = group.GetType();
            var getVolumeGuidMethod = groupType.GetMethod(
                "GetGUIDForVolume",
                InstanceFlags);
            if (getVolumeGuidMethod == null)
            {
                throw new MissingMethodException(
                    groupType.FullName,
                    "GetGUIDForVolume");
            }

            var exposedParameter =
                Activator.CreateInstance(exposedParameterType);
            var guidField = exposedParameterType.GetField(
                "guid",
                InstanceFlags);
            var nameField = exposedParameterType.GetField(
                "name",
                InstanceFlags);

            if (guidField == null || nameField == null)
            {
                throw new MissingFieldException(
                    exposedParameterType.FullName,
                    "guid / name");
            }

            guidField.SetValue(
                exposedParameter,
                getVolumeGuidMethod.Invoke(group, null));
            nameField.SetValue(exposedParameter, parameterName);
            parameters.SetValue(exposedParameter, index);
        }

        static AudioMixerSettings LoadOrCreateSettings()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<AudioMixerSettings>(
                    SETTINGS_PATH);
            if (settings != null)
            {
                return settings;
            }

            settings = ScriptableObject.CreateInstance<
                AudioMixerSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            return settings;
        }

        static void AssignSettings(
            AudioMixerSettings settings,
            AudioMixer mixer,
            AudioMixerGroup masterGroup,
            AudioMixerGroup bgmGroup,
            AudioMixerGroup seGroup,
            AudioMixerGroup uiGroup)
        {
            var serializedSettings =
                new SerializedObject(settings);

            serializedSettings.FindProperty("audioMixer")
                .objectReferenceValue = mixer;
            serializedSettings.FindProperty("masterMixerGroup")
                .objectReferenceValue = masterGroup;
            serializedSettings.FindProperty("bgmMixerGroup")
                .objectReferenceValue = bgmGroup;
            serializedSettings.FindProperty("seMixerGroup")
                .objectReferenceValue = seGroup;
            serializedSettings.FindProperty("uiMixerGroup")
                .objectReferenceValue = uiGroup;

            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        static Type GetEditorType(string fullName)
        {
            var type = typeof(Editor).Assembly.GetType(fullName);
            if (type == null)
            {
                throw new TypeLoadException(
                    $"Unity Editor type not found: {fullName}");
            }

            return type;
        }

        static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parentPath = folderPath.Substring(
                0,
                folderPath.LastIndexOf('/'));
            var folderName = folderPath.Substring(
                folderPath.LastIndexOf('/') + 1);

            EnsureFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
