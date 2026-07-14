using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rail.EditorTools
{
    /// <summary>
    /// 既定テーマ「緑の丘(GreenHills)」を素材(Kenney CC0)から自動構築するツール。
    /// 道マテリアル・スカイボックス・散布設定を生成してStageThemeアセットにまとめる。
    /// 既にあればそれを返すだけなので、ステージ生成ツールから安全に呼べる。
    /// </summary>
    public static class ThemeSetupTool
    {
        const string ThemeFolder = "Assets/Design/Themes";
        const string ThemePath = ThemeFolder + "/Theme_GreenHills.asset";
        const string RoadMaterialPath = ThemeFolder + "/Mat_Road_GreenHills.mat";
        const string SkyboxMaterialPath = ThemeFolder + "/Mat_Skybox_GreenHills.mat";
        const string RoadTexturePath = "Assets/ThirdParty/Kenney/PrototypeTextures/Green/texture_01.png";
        const string NatureModelFolder = "Assets/ThirdParty/Kenney/NatureKit/Models";

        [MenuItem("Tools/Rail/Create Default Theme")]
        public static void CreateFromMenu()
        {
            var theme = EnsureDefaultTheme();
            Selection.activeObject = theme;
        }

        /// <summary>
        /// 既定テーマを取得する。無ければ素材から構築する。
        /// </summary>
        public static StageTheme EnsureDefaultTheme()
        {
            var existing = AssetDatabase.LoadAssetAtPath<StageTheme>(ThemePath);
            if (existing != null) return existing;

            EnsureFolder();

            var theme = ScriptableObject.CreateInstance<StageTheme>();
            theme.roadMaterial = CreateRoadMaterial();
            theme.skyboxMaterial = CreateSkyboxMaterial();
            theme.scatter = CreateScatterEntries();

            theme.enableFog = true;
            theme.fogColor = new Color(0.72f, 0.83f, 0.95f);
            theme.fogDensity = 0.012f;
            theme.sunColor = new Color(1.0f, 0.96f, 0.88f);
            theme.sunEuler = new Vector3(45.0f, -35.0f, 0.0f);
            theme.seed = 20260716;

            AssetDatabase.CreateAsset(theme, ThemePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ThemeSetupTool] 既定テーマを構築しました: {ThemePath}");
            return theme;
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

        static Material CreateRoadMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(RoadMaterialPath);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(RoadTexturePath);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                Debug.LogWarning($"[ThemeSetupTool] 道テクスチャが見つかりません: {RoadTexturePath}");
                material.color = new Color(0.45f, 0.7f, 0.4f);
            }

            // てかりを抑えてマットな見た目に
            material.SetFloat("_Smoothness", 0.05f);

            AssetDatabase.CreateAsset(material, RoadMaterialPath);
            return material;
        }

        static Material CreateSkyboxMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            if (existing != null) return existing;

            var material = new Material(Shader.Find("Skybox/Procedural"));
            material.SetColor("_SkyTint", new Color(0.55f, 0.75f, 0.95f));
            material.SetColor("_GroundColor", new Color(0.55f, 0.65f, 0.55f));
            material.SetFloat("_Exposure", 1.25f);
            material.SetFloat("_AtmosphereThickness", 0.9f);

            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
            return material;
        }

        static List<StageTheme.ScatterEntry> CreateScatterEntries()
        {
            return new List<StageTheme.ScatterEntry>
            {
                new StageTheme.ScatterEntry
                {
                    label = "草",
                    prefabs = LoadModels("grass", "grass_large", "grass_leafs"),
                    densityPer10m = 4.0f,
                    minSideOffset = 2.0f,
                    maxSideOffset = 2.8f,
                    minScale = 1.0f,
                    maxScale = 2.0f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "花",
                    prefabs = LoadModels("flower_redA", "flower_yellowA", "flower_purpleA"),
                    densityPer10m = 2.0f,
                    minSideOffset = 2.0f,
                    maxSideOffset = 2.7f,
                    minScale = 1.0f,
                    maxScale = 1.8f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "低木",
                    prefabs = LoadModels("plant_bushSmall", "plant_bush"),
                    densityPer10m = 1.2f,
                    minSideOffset = 2.2f,
                    maxSideOffset = 2.8f,
                    minScale = 1.0f,
                    maxScale = 1.8f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "岩",
                    prefabs = LoadModels("rock_smallC", "rock_smallF", "rock_smallTopA"),
                    densityPer10m = 0.8f,
                    minSideOffset = 2.2f,
                    maxSideOffset = 2.8f,
                    minScale = 1.0f,
                    maxScale = 1.8f,
                    side = StageTheme.ScatterSide.BothSides,
                },
                new StageTheme.ScatterEntry
                {
                    label = "木(奥側のみ・視界を塞がない)",
                    prefabs = LoadModels("tree_default", "tree_oak", "tree_pineDefaultA", "tree_simple"),
                    densityPer10m = 0.7f,
                    minSideOffset = 2.4f,
                    maxSideOffset = 2.9f,
                    minScale = 1.5f,
                    maxScale = 2.5f,
                    side = StageTheme.ScatterSide.FarSide,
                },
                new StageTheme.ScatterEntry
                {
                    label = "きのこ",
                    prefabs = LoadModels("mushroom_red", "mushroom_tanGroup"),
                    densityPer10m = 0.4f,
                    minSideOffset = 2.1f,
                    maxSideOffset = 2.7f,
                    minScale = 1.0f,
                    maxScale = 1.6f,
                    side = StageTheme.ScatterSide.BothSides,
                },
            };
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
