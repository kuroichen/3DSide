using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rail.EditorTools
{
    /// <summary>
    /// 共有Scene Listとプロジェクト内のシーンを一画面で表示する。
    /// </summary>
    public sealed class SceneListWindow : EditorWindow
    {
        private readonly HashSet<string> selectedPaths = new();
        private string[] projectPaths = Array.Empty<string>();
        private string search = "";
        private Vector2 registeredScroll;
        private Vector2 availableScroll;

        [MenuItem("Tools/Rail/Scene List")]
        private static void Open()
        {
            var window = GetWindow<SceneListWindow>("Scene List");
            window.minSize = new Vector2(500, 450);
        }

        private void OnEnable()
        {
            this.RefreshScenes();
            EditorApplication.projectChanged += this.RefreshScenes;
            EditorBuildSettings.sceneListChanged += this.Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= this.RefreshScenes;
            EditorBuildSettings.sceneListChanged -= this.Repaint;
        }

        private void RefreshScenes()
        {
            this.projectPaths = AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            this.selectedPaths.IntersectWith(this.projectPaths);
            this.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("登録済みシーン（共有Scene List）", EditorStyles.boldLabel);
            var scenes = EditorBuildSettings.scenes;
            var registeredPaths = new HashSet<string>(scenes.Select(scene => scene.path));
            this.selectedPaths.ExceptWith(registeredPaths);

            this.registeredScroll = EditorGUILayout.BeginScrollView(
                this.registeredScroll, GUILayout.Height(170));
            for (var i = 0; i < scenes.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var enabled = EditorGUILayout.Toggle(scenes[i].enabled, GUILayout.Width(20));
                EditorGUILayout.LabelField($"{i}: {scenes[i].path}");
                EditorGUILayout.EndHorizontal();
                if (enabled != scenes[i].enabled)
                {
                    scenes[i].enabled = enabled;
                    EditorBuildSettings.scenes = scenes;
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("開いているシーンを追加"))
                {
                    this.AddOpenScenes();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("プロジェクト内の未登録シーン", EditorStyles.boldLabel);
            this.search = EditorGUILayout.TextField("検索", this.search);
            this.availableScroll = EditorGUILayout.BeginScrollView(this.availableScroll);
            foreach (var path in this.projectPaths)
            {
                if (registeredPaths.Contains(path)
                    || path.IndexOf(this.search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var selected = EditorGUILayout.ToggleLeft(path, this.selectedPaths.Contains(path));
                if (selected)
                {
                    this.selectedPaths.Add(path);
                }
                else
                {
                    this.selectedPaths.Remove(path);
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(this.selectedPaths.Count == 0))
            {
                if (GUILayout.Button($"選択したシーンを追加（{this.selectedPaths.Count}件）"))
                {
                    SceneRegistrationTool.RegisterScenes(this.selectedPaths
                        .OrderBy(path => path, StringComparer.Ordinal).ToArray());
                    this.selectedPaths.Clear();
                }
            }

            EditorGUILayout.HelpBox(
                "共有Scene Listを編集します。独自のScene Listを使用するBuild Profileには反映されません。",
                MessageType.Info);
        }

        private void AddOpenScenes()
        {
            var paths = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path)
                    && AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null)
                {
                    paths.Add(scene.path);
                }
            }

            SceneRegistrationTool.RegisterScenes(paths.ToArray());
            if (paths.Count == 0)
            {
                this.ShowNotification(new GUIContent("保存済みのシーンを開いてください"));
            }
        }
    }
}
