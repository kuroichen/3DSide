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
        const int BuilderVersion = 3; // 見た目の構成を変えたらインクリメント

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

            // フラットカラーで統一(グリッドはデバッグ感の元なので使わない)。
            // 色はKenney素材のミント寄りの緑と喧嘩しないパレットに揃える
            theme.roadMaterial = EnsureColorMaterial("Mat_Road_GreenHills", new Color(0.70f, 0.54f, 0.36f));   // 土の小道
            theme.groundMaterial = EnsureColorMaterial("Mat_Ground_GreenHills", new Color(0.50f, 0.70f, 0.40f)); // 草原
            theme.cliffMaterial = EnsureColorMaterial("Mat_Cliff_GreenHills", new Color(0.48f, 0.38f, 0.28f));  // 崖壁(土の断面)
            theme.nearHillMaterial = EnsureColorMaterial("Mat_Hill_GreenHills", new Color(0.40f, 0.58f, 0.40f));  // 近景の丘
            theme.farMountainMaterial = EnsureColorMaterial("Mat_Mountain_GreenHills", new Color(0.58f, 0.62f, 0.72f)); // 遠景の山
            // 谷底はライティングの影響を受けないUnlitで本当に暗くする
            theme.abyssMaterial = EnsureUnlitMaterial("Mat_Abyss_GreenHills", new Color(0.03f, 0.035f, 0.05f));

            theme.generateGround = true;
            theme.groundWidth = 60.0f;
            theme.groundHolePadding = 1.5f;
            theme.cliffDepth = 14.0f;

            theme.generateHorizon = true;
            theme.horizonPrefabs = LoadModels(
                "rock_tallA", "rock_tallC", "rock_tallE", "rock_tallG",
                "rock_largeA", "rock_largeC");

            theme.skyboxMaterial = EnsureSkyboxMaterial();
            theme.enableFog = true;
            theme.fogColor = new Color(0.70f, 0.78f, 0.90f);
            theme.fogDensity = 0.007f;
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

        /// <summary>
        /// ライティングを受けないUnlit単色マテリアル(谷底など「本当に暗い」表現用)。
        /// </summary>
        static Material EnsureUnlitMaterial(string name, Color color)
        {
            var path = $"{ThemeFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", color);
            material.color = color;
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

            // 地平線が黄緑に転ぶのを防ぐ: 大気は標準の厚み、地面色は彩度低め
            material.SetColor("_SkyTint", new Color(0.45f, 0.65f, 0.90f));
            material.SetColor("_GroundColor", new Color(0.47f, 0.50f, 0.47f));
            material.SetFloat("_Exposure", 1.15f);
            material.SetFloat("_AtmosphereThickness", 1.0f);
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
