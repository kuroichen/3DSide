using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Rail.EditorTools
{
    /// <summary>
    /// 所有アセット「Fantasy landscape」(Pxltiger)からテーマ「Theme_FantasyForest」を構築するツール。
    /// パックのインポート → URPマテリアル変換 → テーマ構築 → Stage1再生成、までを自動で行う。
    /// Library/import_fantasy_theme が存在するとコンパイル直後に自動実行する。
    /// </summary>
    public static class FantasyThemeSetupTool
    {
        const int BuilderVersion = 1;

        const string RequestMarkerPath = "Library/import_fantasy_theme";
        const string PackagePath =
            "~/Library/Unity/Asset Store-5.x/Pxltiger/3D ModelsEnvironments/Fantasy landscape.unitypackage";
        const string PackRoot = "Assets/FantasyEnvironments";
        const string PrefabFolder = PackRoot + "/Environments/Prefabs";
        const string TreeFolder = PackRoot + "/Environments/Ambient-Occlusion-Trees/Prefabs";
        const string TextureFolder = PackRoot + "/Environments/Textures";

        const string ThemeFolder = "Assets/Design/Themes";
        const string ThemePath = ThemeFolder + "/Theme_FantasyForest.asset";

        static bool importRequested = false;

        [MenuItem("Tools/Rail/Import Fantasy Landscape Theme")]
        public static void RunFromMenu()
        {
            File.WriteAllText(RequestMarkerPath, "");
            EditorApplication.update -= RunOnce;
            EditorApplication.update += RunOnce;
        }

        /// <summary>
        /// FantasyForestテーマがあれば返す(無ければnull。自動生成はしない)。
        /// Stage1Creatorがテーマ選択に使う。
        /// </summary>
        public static StageTheme TryLoadTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<StageTheme>(ThemePath);
            if (theme != null && theme.builderVersion >= BuilderVersion) return theme;
            return theme; // 古くてもnullでなければ使う(更新はツール再実行で)
        }

        static void RunStep()
        {
            ConvertMaterialsToUrp();
            BuildTheme();

            Debug.Log($"[FantasyThemeSetupTool] テーマを構築しました: {ThemePath}");

            // 新テーマでStage1を再生成
            Stage1Creator.CreateStage1();
        }

        /// <summary>
        /// パックのマテリアル(Built-in)をURPへ一括変換する。
        /// </summary>
        static void ConvertMaterialsToUrp()
        {
            try
            {
                UnityEditor.Rendering.Universal.Converters.RunInBatchMode(
                    UnityEditor.Rendering.Universal.ConverterContainerId.BuiltInToURP,
                    new List<UnityEditor.Rendering.Universal.ConverterId>
                    {
                        UnityEditor.Rendering.Universal.ConverterId.Material,
                    },
                    UnityEditor.Rendering.Universal.ConverterFilter.Inclusive);
                Debug.Log("[FantasyThemeSetupTool] マテリアルをURPへ変換しました");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FantasyThemeSetupTool] URP変換に失敗(手動で Window > Rendering > Render Pipeline Converter を実行してください): {e.Message}");
            }
        }

        static StageTheme BuildTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<StageTheme>(ThemePath);
            var isNew = theme == null;
            if (isNew)
            {
                theme = ScriptableObject.CreateInstance<StageTheme>();
            }

            theme.builderVersion = BuilderVersion;

            // 道・地面・崖はパックの地面テクスチャを使用
            theme.roadMaterial = EnsureTexturedMaterial("Mat_Road_Fantasy", $"{TextureFolder}/ground_road.png");
            theme.groundMaterial = EnsureTexturedMaterial("Mat_Ground_Fantasy", $"{TextureFolder}/ground_grass.png");
            theme.cliffMaterial = EnsureTexturedMaterial("Mat_Cliff_Fantasy", $"{TextureFolder}/ground_soil.png");

            // 遠景は引き続き単色シルエット(フォグに溶かす)
            theme.nearHillMaterial = ThemeSetupTool.EnsureColorMaterial("Mat_Hill_Fantasy", new Color(0.32f, 0.45f, 0.33f));
            theme.farMountainMaterial = ThemeSetupTool.EnsureColorMaterial("Mat_Mountain_Fantasy", new Color(0.52f, 0.56f, 0.66f));
            theme.abyssMaterial = ThemeSetupTool.EnsureUnlitMaterial("Mat_Abyss_GreenHills", new Color(0.07f, 0.09f, 0.13f));

            theme.generateGround = true;
            theme.groundWidth = 60.0f;
            theme.groundHolePadding = 1.5f;
            theme.cliffDepth = 14.0f;

            theme.generateHorizon = true;
            theme.horizonPrefabs = LoadPrefabs(PrefabFolder, "Rock1", "Rock2", "Rock3");

            theme.skyboxMaterial = ThemeSetupTool.EnsureSkyboxMaterial();
            theme.postProcessProfile = ThemeSetupTool.EnsureVolumeProfile();
            theme.enableFog = true;
            theme.fogColor = new Color(0.80f, 0.86f, 0.92f);
            theme.fogDensity = 0.008f;
            theme.sunColor = new Color(1.0f, 0.96f, 0.88f);
            theme.sunEuler = new Vector3(45.0f, -35.0f, 0.0f);
            theme.seed = 20260716;

            theme.scatter = new List<StageTheme.ScatterEntry>
            {
                new StageTheme.ScatterEntry
                {
                    label = "草・シダ",
                    prefabs = LoadPrefabs(PrefabFolder, "Grass1", "Grass2", "Grass3", "Grass4", "Fern1", "Fern2"),
                    densityPer10m = 6.0f,
                    minSideOffset = 2.5f,
                    maxSideOffset = 14.0f,
                    minScale = 0.9f,
                    maxScale = 1.6f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "花",
                    prefabs = LoadPrefabs(PrefabFolder, "Flower1", "Flower3", "Flower5", "Flower7"),
                    densityPer10m = 2.5f,
                    minSideOffset = 3.0f,
                    maxSideOffset = 12.0f,
                    minScale = 0.9f,
                    maxScale = 1.4f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "低木・植物",
                    prefabs = LoadPrefabs(PrefabFolder, "Bush1", "Plant2", "Plant4"),
                    densityPer10m = 1.2f,
                    minSideOffset = 4.0f,
                    maxSideOffset = 14.0f,
                    minScale = 0.9f,
                    maxScale = 1.5f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "岩",
                    prefabs = LoadPrefabs(PrefabFolder, "Stone1", "Stone2", "Stone3"),
                    densityPer10m = 0.6f,
                    minSideOffset = 4.0f,
                    maxSideOffset = 16.0f,
                    minScale = 0.8f,
                    maxScale = 1.5f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "木(奥側のみ)",
                    prefabs = LoadPrefabs(TreeFolder,
                        "Oak_tree1", "Oak_tree2", "Birch_tree1", "Birch_tree2",
                        "Deciduous_tree1", "Deciduous_tree2"),
                    densityPer10m = 1.0f,
                    minSideOffset = 6.0f,
                    maxSideOffset = 20.0f,
                    minScale = 0.8f,
                    maxScale = 1.3f,
                    side = StageTheme.ScatterSide.FarSide,
                },
                new StageTheme.ScatterEntry
                {
                    label = "きのこ",
                    prefabs = LoadPrefabs(PrefabFolder, "Mushroom1", "Mushroom2", "Mushroom3"),
                    densityPer10m = 0.4f,
                    minSideOffset = 3.0f,
                    maxSideOffset = 10.0f,
                    minScale = 0.9f,
                    maxScale = 1.4f,
                    side = StageTheme.ScatterSide.BothSides,
                },
            };

            if (isNew)
            {
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            else
            {
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();

            return theme;
        }

        static Material EnsureTexturedMaterial(string name, string texturePath)
        {
            var path = $"{ThemeFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
            }
            else
            {
                Debug.LogWarning($"[FantasyThemeSetupTool] テクスチャが見つかりません: {texturePath}");
            }

            material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);

            return material;
        }

        static GameObject[] LoadPrefabs(string folder, params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var name in names)
            {
                var path = $"{folder}/{name}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    list.Add(prefab);
                }
                else
                {
                    Debug.LogWarning($"[FantasyThemeSetupTool] プレハブが見つかりません: {path}");
                }
            }
            return list.ToArray();
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            EditorApplication.update += RunOnce;
        }

        static void RunOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(RequestMarkerPath))
            {
                EditorApplication.update -= RunOnce;
                return;
            }

            // ① パック未導入ならインポートを一度だけ発行し、完了(フォルダ出現)を待つ
            if (!AssetDatabase.IsValidFolder(PackRoot))
            {
                if (!File.Exists(PackagePath))
                {
                    Debug.LogWarning($"[FantasyThemeSetupTool] パッケージが見つかりません: {PackagePath}");
                    File.Delete(RequestMarkerPath);
                    EditorApplication.update -= RunOnce;
                    return;
                }

                if (!importRequested)
                {
                    importRequested = true;
                    Debug.Log("[FantasyThemeSetupTool] Fantasy landscapeをインポートします(112MB・少し時間がかかります)");
                    AssetDatabase.ImportPackage(PackagePath, false);
                }
                return;
            }

            // ② インポート済み → マーカーを消費して構築へ
            EditorApplication.update -= RunOnce;
            File.Delete(RequestMarkerPath);

            try
            {
                RunStep();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
