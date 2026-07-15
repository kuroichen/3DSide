using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rail.EditorTools
{
    /// <summary>
    /// 既定テーマ「緑の丘(GreenHills)」を素材(Kenney CC0)から自動構築するツール。
    /// フラットカラーのマテリアル・地面・遠景・散布設定を生成してStageThemeにまとめる。
    /// バージョンが古いテーマは自動で作り直す(アセットの参照は維持)。
    /// </summary>
    public static class ThemeSetupTool
    {
        const int BuilderVersion = 2; // 見た目の構成を変えたらインクリメント

        const string ThemeFolder = "Assets/Design/Themes";
        const string ThemePath = ThemeFolder + "/Theme_GreenHills.asset";
        const string NatureModelFolder = "Assets/ThirdParty/Kenney/NatureKit/Models";

        [MenuItem("Tools/Rail/Create Default Theme")]
        public static void CreateFromMenu()
        {
            var theme = EnsureDefaultTheme();
            Selection.activeObject = theme;
        }

        /// <summary>
        /// 既定テーマを取得する。無ければ構築、古ければ内容を作り直す。
        /// </summary>
        public static StageTheme EnsureDefaultTheme()
        {
            EnsureFolder();

            var theme = AssetDatabase.LoadAssetAtPath<StageTheme>(ThemePath);
            var isNew = theme == null;

            if (isNew)
            {
                theme = ScriptableObject.CreateInstance<StageTheme>();
            }
            else if (theme.builderVersion >= BuilderVersion)
            {
                return theme;
            }

            Build(theme);

            if (isNew)
            {
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            else
            {
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"[ThemeSetupTool] 既定テーマを構築しました(v{BuilderVersion}): {ThemePath}");
            return theme;
        }

        static void Build(StageTheme theme)
        {
            theme.builderVersion = BuilderVersion;

            // フラットカラーで統一(グリッドはデバッグ感の元なので使わない)
            theme.roadMaterial = EnsureColorMaterial("Mat_Road_GreenHills", new Color(0.62f, 0.48f, 0.33f));   // 土の小道
            theme.groundMaterial = EnsureColorMaterial("Mat_Ground_GreenHills", new Color(0.42f, 0.62f, 0.32f)); // 草原
            theme.nearHillMaterial = EnsureColorMaterial("Mat_Hill_GreenHills", new Color(0.33f, 0.50f, 0.32f));  // 近景の丘
            theme.farMountainMaterial = EnsureColorMaterial("Mat_Mountain_GreenHills", new Color(0.52f, 0.58f, 0.70f)); // 遠景の山
            theme.abyssMaterial = EnsureColorMaterial("Mat_Abyss_GreenHills", new Color(0.07f, 0.08f, 0.11f));  // 谷底

            theme.generateGround = true;
            theme.groundWidth = 60.0f;
            theme.groundHolePadding = 1.5f;

            theme.generateHorizon = true;
            theme.horizonPrefabs = LoadModels(
                "rock_tallA", "rock_tallC", "rock_tallE", "rock_tallG",
                "rock_largeA", "rock_largeC");

            theme.skyboxMaterial = EnsureSkyboxMaterial();
            theme.enableFog = true;
            theme.fogColor = new Color(0.72f, 0.82f, 0.94f);
            theme.fogDensity = 0.008f;
            theme.sunColor = new Color(1.0f, 0.96f, 0.88f);
            theme.sunEuler = new Vector3(45.0f, -35.0f, 0.0f);
            theme.seed = 20260716;

            // 地面ができたので散布は大地全体に広げる。
            // 手前(カメラ側)は背の低い物だけにして視界を守る
            theme.scatter = new List<StageTheme.ScatterEntry>
            {
                new StageTheme.ScatterEntry
                {
                    label = "草",
                    prefabs = LoadModels("grass", "grass_large", "grass_leafs"),
                    densityPer10m = 6.0f,
                    minSideOffset = 2.5f,
                    maxSideOffset = 14.0f,
                    minScale = 1.2f,
                    maxScale = 2.2f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "花",
                    prefabs = LoadModels("flower_redA", "flower_yellowA", "flower_purpleA"),
                    densityPer10m = 2.5f,
                    minSideOffset = 3.0f,
                    maxSideOffset = 12.0f,
                    minScale = 1.0f,
                    maxScale = 1.8f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "低木",
                    prefabs = LoadModels("plant_bushSmall", "plant_bush", "plant_bushLarge"),
                    densityPer10m = 1.5f,
                    minSideOffset = 4.0f,
                    maxSideOffset = 14.0f,
                    minScale = 1.2f,
                    maxScale = 2.0f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "岩",
                    prefabs = LoadModels("rock_smallC", "rock_smallF", "rock_smallTopA"),
                    densityPer10m = 0.8f,
                    minSideOffset = 4.0f,
                    maxSideOffset = 16.0f,
                    minScale = 1.0f,
                    maxScale = 2.0f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "木(奥側のみ・大きく)",
                    prefabs = LoadModels("tree_default", "tree_oak", "tree_pineDefaultA", "tree_simple", "tree_fat"),
                    densityPer10m = 1.2f,
                    minSideOffset = 6.0f,
                    maxSideOffset = 20.0f,
                    minScale = 2.2f,
                    maxScale = 3.8f,
                    side = StageTheme.ScatterSide.FarSide,
                },
                new StageTheme.ScatterEntry
                {
                    label = "きのこ",
                    prefabs = LoadModels("mushroom_red", "mushroom_tanGroup"),
                    densityPer10m = 0.4f,
                    minSideOffset = 3.0f,
                    maxSideOffset = 10.0f,
                    minScale = 1.0f,
                    maxScale = 1.6f,
                    side = StageTheme.ScatterSide.BothSides,
                },
            };
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Design"))
            {
                AssetDatabase.CreateFolder("Assets", "Design");
            }
            if (!AssetDatabase.IsValidFolder(ThemeFolder))
            {
                AssetDatabase.CreateFolder("Assets/Design", "Themes");
            }
        }

        /// <summary>
        /// 単色マテリアルを取得または作成する(既存なら色だけ更新)。
        /// </summary>
        static Material EnsureColorMaterial(string name, Color color)
        {
            var path = $"{ThemeFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", null);
            material.SetColor("_BaseColor", color);
            material.color = color;
            material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);

            return material;
        }

        static Material EnsureSkyboxMaterial()
        {
            var path = $"{ThemeFolder}/Mat_Skybox_GreenHills.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_SkyTint", new Color(0.55f, 0.75f, 0.95f));
            material.SetColor("_GroundColor", new Color(0.50f, 0.62f, 0.50f));
            material.SetFloat("_Exposure", 1.25f);
            material.SetFloat("_AtmosphereThickness", 0.9f);
            EditorUtility.SetDirty(material);

            return material;
        }

        static GameObject[] LoadModels(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var name in names)
            {
                var path = $"{NatureModelFolder}/{name}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                {
                    list.Add(model);
                }
                else
                {
                    Debug.LogWarning($"[ThemeSetupTool] モデルが見つかりません: {path}");
                }
            }
            return list.ToArray();
        }
    }
}
