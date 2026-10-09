using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rail.EditorTools
{
    /// <summary>
    /// 選択したシーンを開かずに共有Scene Listへ登録する。
    /// </summary>
    public static class SceneRegistrationTool
    {
        private const string MENU_PATH = "Tools/Rail/Register Selected Scenes";

        /// <summary>
        /// 既存の順序を保持して未登録シーンを追加し、登録済みシーンは有効にする。
        /// </summary>
        [MenuItem(MENU_PATH)]
        public static void RegisterSelectedScenes()
        {
            RegisterScenes(Selection.objects
                .OfType<SceneAsset>()
                .Select(AssetDatabase.GetAssetPath)
                .Distinct()
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// 指定したシーンを共有Scene Listへ追加する。
        /// </summary>
        public static void RegisterScenes(string[] paths)
        {
            paths = paths.Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();

            var scenes = EditorBuildSettings.scenes.ToList();
            var addedCount = 0;
            var enabledCount = 0;

            foreach (var path in paths)
            {
                var existing = scenes.FirstOrDefault(scene => scene.path == path);
                if (existing == null)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    addedCount++;
                }
                else if (!existing.enabled)
                {
                    existing.enabled = true;
                    enabledCount++;
                }
            }

            if (addedCount > 0 || enabledCount > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Debug.Log($"[SceneRegistrationTool]共有Scene List: {addedCount}件追加、"
                + $"{enabledCount}件有効化、{paths.Length - addedCount - enabledCount}件登録済み。");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool CanRegisterSelectedScenes()
        {
            return Selection.objects.OfType<SceneAsset>().Any();
        }
    }
}
