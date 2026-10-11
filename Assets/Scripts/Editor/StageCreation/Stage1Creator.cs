using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace Rail.EditorTools
{
    /// <summary>
    /// 1ステージ目(要素見本市)を自動生成するメニュー。すり抜け床は不使用(全て通常コリジョン)。
    /// 設計の正は Docs/specs/Stage1新設計.md(レベルデザイン法則集準拠)。
    /// エリア1(x=0〜135) → 遷移扉 → エリア2(x=200〜310) の2エリア構成。
    /// [エリア1]
    /// ① 穴あき道: 2 / 2.5 / 3m の穴を跳んで渡る。最初の2m穴だけは「安全穴」
    ///    (地面が残っていて落ちても死なず、登って復帰できる。法則D: 穴の安全な初登場)
    /// ② 三層分岐: 上の道はz=7 / z=14〜18の別レーンを走らせ、本筋の頭上に道を置かない
    ///    - 丘(y3.0)から前方3m・+2.0mジャンプで2段目(y5.0)へ。入口の直下は「窪地」
    ///      (道に穴・地面は残す)にして、頭上クリアランス4.55mの確保と分岐失敗の受け皿を兼ねる
    ///    - 2段目はゴール前平地の上空(y4.7)で終わり、飛び降りて合流(上下分離型・一方通行)
    ///    - 2段目のこぶ(y7.0)から前方3m・+2.4mで3段目(y9.4〜10.5)。逆向きSplineで
    ///      t=1.0側から乗る検証を兼ねる。裏ゴールで行き止まり
    /// ③ 高低差の飛び石(1段目のみ): 低→中→高→中→低。登りは3m間隔、下りは4m間隔
    /// [エリア2] 穴あき3連 → 丘(y3.0)から上レーン分岐(穴2つをスキップ) → 飛び降り合流 → ゴール
    /// 寸法はジャンプ性能(跳頂3.34m/飛距離限界4.5m)から逆算。
    /// 頭上クリアランスは最低3.4m(跳頂超)を全直上区間で確保する(アンチパターン#1)。
    /// 穴・トリガーの位置は設計座標(x)で指定し、弧長(railZ)へ変換して適用する。
    /// イベント: StageDefinition + AreaTrigger一式を配線済み
    ///  - エリア1末尾(x132)の扉でエリア2へ遷移。ゴール(x298)/ステージ出口(x304)はエリア2の末尾
    ///  - 裏ゴール(3段目高台) / 中間地点(エリア1 x78・エリア2入口 x204)
    ///  - 隠し部屋: x70頭上のジャンプトリガー → BonusArea(z+70の小部屋) → 出口で本筋x70へ帰還
    ///  - エリアはArea1/Area2/BonusのContentsをStageAreaで包み、切替は中身の活性で行う
    /// Library/create_stage1_request が存在する場合はコンパイル直後に一度だけ自動実行する(外部ツール連携用)。
    /// </summary>
    public static class Stage1Creator
    {
        const string RequestMarkerPath = "Library/create_stage1_request";
        const string ScenePath = "Assets/Scenes/World1_Stage1.unity";
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        const string CharacterTestScenePath = "Assets/Scenes/CharacterTestScene.unity";

        [MenuItem("Tools/Rail/Create Stage 1")]
        public static void CreateStage1()
        {
            // 敵プレハブを用意(無ければCharacterTestSceneのEnemyから抽出)
            var enemyPrefab = EnsureEnemyPrefab();

            // 新しいシーンを作成して保存
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // --- メインルート(下) ---
            // x=28の崖の縁まで平坦。ジャンプでギャップ(3m)を跳び越えれば上ルート、
            // 跳ばなければそのまま谷(y=-1.9)へ落ちて下ルート(落ちること自体がルート選択)
            // 寸法はジャンプ性能(ギャップ限界4.5m/跳頂3.34m)の6〜7割を要求する1面相当
            // --- 1段目: 地上ルート ---
            // ①穴あき → ②丘(2段目への踏切)→ 2・3段目の下をくぐる → ③飛び石 → ゴール平地
            var main = CreateSpline("Stage1MainSpline", new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(14.0f, 0.0f, 0.0f),
                new Vector3(41.0f, 0.0f, 0.0f),
                new Vector3(48.0f, 3.0f, 0.0f),   // 丘(2段目への踏切)。y3.0で入口の頭上クリアランスを確保
                new Vector3(55.0f, 3.0f, 0.0f),   // 丘の縁(ここから前方3m・+2.0mを跳ぶ)
                new Vector3(58.0f, 0.3f, 0.0f),   // 窪地(x55.5〜60.5は道なし。失敗したら草地に着地)
                new Vector3(61.0f, 0.0f, 0.0f),
                new Vector3(80.0f, 0.0f, 0.0f),
                new Vector3(89.5f, 1.5f, 0.0f),   // 飛び石: 中
                new Vector3(97.5f, 3.0f, 0.0f),   // 飛び石: 高
                new Vector3(106.5f, 1.5f, 0.0f),  // 飛び石: 中
                new Vector3(115.5f, 0.0f, 0.0f),  // 飛び石: 低(着地)
                new Vector3(135.0f, 0.0f, 0.0f),  // ゴール平地
            });

            // --- 2段目 ---
            // 丘からジャンプで乗る。z=7の別レーンを走らせ、本筋の頭上には道を置かない
            // (頭上に道があると本筋でジャンプできなくなるため)。本筋の真上に来るのは
            // 入口と合流の先端だけ。合流は「本筋の上空で終わって飛び降りる」上下分離型。
            // 端点を本筋に接触させると最近傍判定に吸い付かれるため、分岐と同じ形で終わらせる
            var middle = CreateSpline("Stage1MiddleSpline", new[]
            {
                new Vector3(58.0f, 5.0f, 0.0f),   // 入口(丘の縁から3m先・+2.0m)。ここだけ本筋(窪地)の真上
                new Vector3(65.0f, 5.0f, 7.0f),   // すぐ奥のレーンへ逃がす
                new Vector3(71.0f, 5.1f, 7.0f),
                new Vector3(74.0f, 7.0f, 7.0f),   // 3段目への踏切(こぶ)。y7.0で3段目入口の頭上3.6mを確保
                new Vector3(77.0f, 7.0f, 7.0f),   // こぶの縁(ここから前方3m・+2.4mを跳ぶと3段目)
                new Vector3(80.0f, 5.0f, 7.0f),
                new Vector3(96.0f, 4.9f, 7.0f),
                new Vector3(112.0f, 4.8f, 7.0f),
                new Vector3(118.0f, 4.75f, 4.0f), // 合流のときだけ本筋へ寄る
                new Vector3(122.0f, 4.7f, 0.0f),  // 本筋のゴール前平地の上空で終わる(飛び降り合流)
            });

            // --- 3段目(逆向きSpline) ---
            // 進行方向と反対向きに定義し、t=1.0側の端から乗る(合流に1.0側から乗る検証)。
            // 2段目のこぶから前方上へジャンプして先端に乗り、振り返って逆走方向へ進む。
            // z=14〜18の奥レーンを走らせ、本筋や丘の頭上には被せない。裏ゴールの高台で行き止まり
            var top = CreateSpline("Stage1TopSpline", new[]
            {
                new Vector3(22.0f, 10.5f, 18.0f), // t=0: 裏ゴール(行き止まりの高台)
                new Vector3(34.0f, 10.3f, 18.0f),
                new Vector3(48.0f, 10.1f, 16.0f),
                new Vector3(62.0f, 9.9f, 14.0f),
                new Vector3(76.0f, 9.6f, 11.0f),
                new Vector3(80.0f, 9.4f, 7.5f),   // t=1: 入口(2段目のこぶの縁から3m先・+2.4m)
            });

            // --- 2エリア目(x=200〜) ---
            // エリア1末尾の扉から遷移してくる後半戦。
            // 穴あき3連 → 丘から上レーンへ分岐(穴2つをスキップするご褒美) → 飛び降り合流 → ゴール
            var main2 = CreateSpline("Stage2MainSpline", new[]
            {
                new Vector3(200.0f, 0.0f, 0.0f),
                new Vector3(216.0f, 0.0f, 0.0f),
                new Vector3(240.0f, 0.0f, 0.0f),
                new Vector3(247.0f, 3.0f, 0.0f),  // 丘(上レーンへの踏切)。y3.0はエリア1と同じ理由
                new Vector3(251.0f, 3.0f, 0.0f),  // 丘の縁(ここから前方3m・+2.0mを跳ぶ)
                new Vector3(254.0f, 0.3f, 0.0f),  // 窪地(x251.5〜256.5は道なし)
                new Vector3(257.0f, 0.0f, 0.0f),
                new Vector3(276.0f, 0.0f, 0.0f),
                new Vector3(296.0f, 0.0f, 0.0f),
                new Vector3(310.0f, 0.0f, 0.0f),  // ゴール平地
            });

            var sub2 = CreateSpline("Stage2SubSpline", new[]
            {
                new Vector3(254.0f, 5.0f, 0.0f),  // 入口(丘の縁から3m先・+2.0m)。ここだけ本筋(窪地)の真上
                new Vector3(261.0f, 5.0f, 7.0f),  // すぐ奥のレーンへ逃がす
                new Vector3(275.0f, 4.9f, 7.0f),
                new Vector3(287.0f, 4.8f, 5.0f),
                new Vector3(291.0f, 4.7f, 0.0f),  // ゴール平地の上空で終わる(飛び降り合流)
            });

            Debug.Log("[Stage1Creator] Splineを生成しました。Stage Auto Builderを実行します。");

            // ステージ一式を生成(レール・ゲート・スポーン・マネージャ)。
            // 道は穴あき・すり抜け対応のため自前で敷くので、ビルダー側の道生成はOFF
            var window = EditorWindow.GetWindow<StageAutoBuilderWindow>("Stage Auto Builder");
            window.SetMainSpline(main);
            window.SetRoadEnabled(false);
            window.Build();

            var root = GameObject.Find("GeneratedStage");
            var rootTransform = root != null ? root.transform : null;

            // --- エリア構造 ---
            // メインエリアの中身はArea1Contents配下に束ね、エリア切替で丸ごと休眠できるようにする。
            // マネージャ類・スポーン地点・StageDefinitionはエリア外(常時生存)としてルート直下に置く
            var area1Contents = new GameObject("Area1Contents");
            Undo.RegisterCreatedObjectUndo(area1Contents, "Create Stage1");
            area1Contents.transform.SetParent(rootTransform);

            var area1 = CreateArea("Area1", area1Contents, rootTransform);

            var bonusContents = new GameObject("BonusContents");
            Undo.RegisterCreatedObjectUndo(bonusContents, "Create Stage1");
            bonusContents.transform.SetParent(rootTransform);

            var bonusArea = CreateArea("BonusArea", bonusContents, rootTransform);

            // 2エリア目(x=200〜)の中身もArea2Contents配下に束ねる
            var area2Contents = new GameObject("Area2Contents");
            Undo.RegisterCreatedObjectUndo(area2Contents, "Create Stage1");
            area2Contents.transform.SetParent(rootTransform);

            var area2 = CreateArea("Area2", area2Contents, rootTransform);

            // Splineもエリアの中身(休眠時にレール登録が解除される)
            main.transform.SetParent(area1Contents.transform);
            middle.transform.SetParent(area1Contents.transform);
            top.transform.SetParent(area1Contents.transform);
            main2.transform.SetParent(area2Contents.transform);
            sub2.transform.SetParent(area2Contents.transform);

            // 以降のエリア1の生成物はすべてArea1Contents配下へ
            var parent = area1Contents.transform;
            // 以降のエリア2の生成物はすべてArea2Contents配下へ
            var parent2 = area2Contents.transform;

            // --- テーマ(見た目)を用意 ---
            // FantasyForest(所有アセット製)があれば優先、無ければ既定の緑の丘
            var theme = FantasyThemeSetupTool.TryLoadTheme();
            if (theme == null)
            {
                theme = ThemeSetupTool.EnsureDefaultTheme();
            }
            var roadMaterial = theme != null ? theme.roadMaterial : null;

            // --- 道を敷く(幅・厚みはジャンプ性能のスケール感に合わせる) ---
            // 穴はx座標で指定し、弧長(railZ)へ変換してから開ける(丘の勾配で1m前後ズレるため)。
            // 「安全穴」= 道だけ開けて地面は残す穴。落ちても草地(y-0.35)に着地し、登って復帰できる
            var mainSafeHolesX = new[]
            {
                new Vector2(16.0f, 18.0f),    // ① 2.0m: 穴の初登場は死なない形で見せる(法則D)
                new Vector2(55.5f, 60.5f),    // 分岐入口下の窪地(失敗の受け皿+頭上クリアランス4.55m)
            };
            var mainDeadlyHolesX = new[]
            {
                new Vector2(24.0f, 26.5f),    // ① 2.5m(ここから本番の谷)
                new Vector2(33.0f, 36.0f),    // ① 3.0m
                new Vector2(84.5f, 87.5f),    // ③ 登り3m
                new Vector2(92.5f, 95.5f),    // ③ 登り3m
                new Vector2(101.0f, 105.0f),  // ③ 下り4m
                new Vector2(110.0f, 114.0f),  // ③ 下り4m
            };
            var mainRoad = StageGeometryUtility.LayCubes(
                main, 2.0f, 6.0f, 0.8f, true, parent,
                HolesAtX(main, Concat(mainSafeHolesX, mainDeadlyHolesX)), roadMaterial);
            Undo.AddComponent<GeneratedStageContent>(mainRoad);

            // 2段目・3段目: 通常コリジョンの道(すり抜けは不使用。乗り口は互い違い配置で
            // 下の層の歩行帯に被さらないため、頭上の引っかかりは起きない)
            var middleRoad = StageGeometryUtility.LayCubes(
                middle, 2.0f, 6.0f, 0.8f, true, parent, null, roadMaterial);
            Undo.AddComponent<GeneratedStageContent>(middleRoad);

            var topRoad = StageGeometryUtility.LayCubes(
                top, 2.0f, 6.0f, 0.8f, true, parent, null, roadMaterial);
            Undo.AddComponent<GeneratedStageContent>(topRoad);

            // --- テーマ適用(大地・遠景・散布・環境) ---
            if (theme != null)
            {
                // 大地: メインルート沿いに広い地面を敷く。道の穴は少し広めに裂いて「谷」にする
                if (theme.generateGround)
                {
                    // 地面の穴(谷)は死の穴だけ。安全穴・窪地の下には地面を残す
                    var groundHoles = PadHoles(HolesAtX(main, mainDeadlyHolesX), theme.groundHolePadding);
                    var ground = StageGeometryUtility.GenerateGroundRibbon(
                        main, theme.groundWidth, -0.35f, groundHoles, theme.groundMaterial, parent,
                        theme.cliffMaterial, theme.cliffDepth);
                    Undo.AddComponent<GeneratedStageContent>(ground);
                }

                // 遠景: 丘と山のシルエット+谷底の暗い床
                if (theme.generateHorizon)
                {
                    var horizon = StageDecorator.GenerateHorizon(main, theme, parent);
                    Undo.AddComponent<GeneratedStageContent>(horizon);
                }

                // 散布は地面・道のコライダーに足場を求めるため、それらの生成後に行う
                Physics.SyncTransforms();

                var mainDeco = StageDecorator.Scatter(main, theme, parent);
                Undo.AddComponent<GeneratedStageContent>(mainDeco);

                var middleDeco = StageDecorator.Scatter(middle, theme, parent);
                Undo.AddComponent<GeneratedStageContent>(middleDeco);

                var topDeco = StageDecorator.Scatter(top, theme, parent);
                Undo.AddComponent<GeneratedStageContent>(topDeco);

                StageDecorator.ApplyEnvironment(theme);

                // 絵作り(ポストプロセス)はエリア切替に影響されないようルート直下へ
                var postProcess = StageDecorator.ApplyPostProcess(theme);
                if (postProcess != null)
                {
                    postProcess.transform.SetParent(rootTransform);
                    Undo.AddComponent<GeneratedStageContent>(postProcess);
                }
            }

            // カメラはジャンプの放物線全体が見える距離まで引く
            var followCamera = Camera.main != null
                ? Camera.main.GetComponent<SplineFollowCamera>()
                : null;
            if (followCamera != null)
            {
                var cameraSo = new SerializedObject(followCamera);
                cameraSo.FindProperty("offset").vector3Value = new Vector3(0.0f, 3.0f, 14.0f);
                cameraSo.ApplyModifiedProperties();
            }

            // 敵の出現地点を配置(地上中盤・ゴール手前、2段目に1体)。実体は実行時にStageAreaが生成する。
            // 合流点(x122)の直下±数mには置かない(アンチパターン#3)
            var area1EnemySpawns = new List<EnemySpawnPoint>();
            var area2EnemySpawns = new List<EnemySpawnPoint>();
            if (enemyPrefab != null)
            {
                area1EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, main, TAtX(main, 74.0f), parent));
                area1EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, main, TAtX(main, 128.0f), parent));
                area1EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, middle, 0.5f, parent));
            }
            else
            {
                Debug.LogWarning("[Stage1Creator] 敵プレハブを用意できなかったため、敵の配置をスキップしました。");
            }

            // --- エリア2の道を敷く(穴あき3連 → 丘から上レーンへ分岐) ---
            // エリア2は応用編のため安全穴なし(窪地のみ)。穴はワールドx座標で指定
            var main2SafeHolesX = new[]
            {
                new Vector2(251.5f, 256.5f),  // 分岐入口下の窪地
            };
            var main2DeadlyHolesX = new[]
            {
                new Vector2(206.0f, 208.0f),  // 2.0m
                new Vector2(222.0f, 224.5f),  // 2.5m
                new Vector2(230.0f, 233.0f),  // 3.0m
                new Vector2(262.5f, 265.5f),  // 3.0m
                new Vector2(274.5f, 278.5f),  // 4.0m(上レーンを選べば跳ばずに済む)
            };
            var main2Road = StageGeometryUtility.LayCubes(
                main2, 2.0f, 6.0f, 0.8f, true, parent2,
                HolesAtX(main2, Concat(main2SafeHolesX, main2DeadlyHolesX)), roadMaterial);
            Undo.AddComponent<GeneratedStageContent>(main2Road);

            var sub2Road = StageGeometryUtility.LayCubes(
                sub2, 2.0f, 6.0f, 0.8f, true, parent2, null, roadMaterial);
            Undo.AddComponent<GeneratedStageContent>(sub2Road);

            // --- エリア2のテーマ適用(大地・遠景・散布) ---
            if (theme != null)
            {
                if (theme.generateGround)
                {
                    var main2GroundHoles = PadHoles(HolesAtX(main2, main2DeadlyHolesX), theme.groundHolePadding);
                    var ground2 = StageGeometryUtility.GenerateGroundRibbon(
                        main2, theme.groundWidth, -0.35f, main2GroundHoles, theme.groundMaterial, parent2,
                        theme.cliffMaterial, theme.cliffDepth);
                    Undo.AddComponent<GeneratedStageContent>(ground2);
                }

                if (theme.generateHorizon)
                {
                    var horizon2 = StageDecorator.GenerateHorizon(main2, theme, parent2);
                    Undo.AddComponent<GeneratedStageContent>(horizon2);
                }

                Physics.SyncTransforms();

                var main2Deco = StageDecorator.Scatter(main2, theme, parent2);
                Undo.AddComponent<GeneratedStageContent>(main2Deco);

                var sub2Deco = StageDecorator.Scatter(sub2, theme, parent2);
                Undo.AddComponent<GeneratedStageContent>(sub2Deco);
            }

            // エリア2にも敵の出現地点を配置。合流点(x291)の直下±数mを避ける(アンチパターン#3)
            if (enemyPrefab != null)
            {
                area2EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, main2, TAtX(main2, 238.0f), parent2));
                area2EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, main2, TAtX(main2, 296.0f), parent2));
                area2EnemySpawns.Add(CreateEnemySpawnPoint(enemyPrefab, sub2, 0.5f, parent2));
            }

            // --- ボーナス部屋(エリア切替のデモ。z+70の離れた場所にある安全な小部屋) ---
            var bonusSpline = CreateSpline("BonusSpline", new[]
            {
                new Vector3(60.0f, 0.0f, 70.0f),
                new Vector3(84.0f, 0.0f, 70.0f),
            });
            bonusSpline.transform.SetParent(bonusContents.transform);
            Undo.AddComponent<SplineRail>(bonusSpline.gameObject);
            StageGeometryUtility.LayCubes(
                bonusSpline, 2.0f, 6.0f, 0.8f, true, bonusContents.transform, null, roadMaterial);

            // スポーン/復帰地点はエリア休眠の影響を受けないようルート直下に置く
            var bonusSpawn = CreateAnchor("BonusSpawnPoint", bonusSpline, 0.1f, rootTransform);
            var bonusReturn = CreateAnchor("BonusReturnPoint", main, TAtX(main, 70.0f), rootTransform);

            // --- イベントトリガー一式 ---
            var triggers = new List<AreaTrigger>();

            // 通常ゴール(エリア2の終端手前)
            var goalTrigger = CreateEventTrigger(
                "Goal", main2, TAtX(main2, 298.0f), new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent2);
            WireTriggerInfo(goalTrigger, Undo.AddComponent<GoalInfo>(goalTrigger.gameObject));
            triggers.Add(goalTrigger);

            // 裏ゴール(3段目の行き止まり高台)
            var secretTrigger = CreateEventTrigger(
                "SecretGoal", top, 0.02f, new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent);
            var secretInfo = Undo.AddComponent<GoalInfo>(secretTrigger.gameObject);
            var secretSo = new SerializedObject(secretInfo);
            secretSo.FindProperty("goalType").enumValueIndex = (int)GoalType.Secret;
            secretSo.ApplyModifiedProperties();
            WireTriggerInfo(secretTrigger, secretInfo);
            triggers.Add(secretTrigger);

            // 中間地点(飛び石の手前 x=78)
            var checkpointRespawn = CreateAnchor(
                "CheckpointRespawn", main, TAtX(main, 78.0f), rootTransform);
            var checkpointTrigger = CreateEventTrigger(
                "Checkpoint", main, TAtX(main, 78.0f), new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent);
            var checkpointInfo = Undo.AddComponent<CheckPointInfo>(checkpointTrigger.gameObject);
            var checkpointSo = new SerializedObject(checkpointInfo);
            checkpointSo.FindProperty("checkPointId").stringValue = "cp-01";
            checkpointSo.FindProperty("respawnPoint").objectReferenceValue = checkpointRespawn;
            checkpointSo.ApplyModifiedProperties();
            WireTriggerInfo(checkpointTrigger, checkpointInfo);
            triggers.Add(checkpointTrigger);

            // 隠し部屋の入口(x=70の頭上。ジャンプしないと触れない高さ)
            var bonusEntryTrigger = CreateEventTrigger(
                "BonusEntry", main, TAtX(main, 70.0f), new Vector3(2.5f, 1.5f, 1.5f), 2.6f, parent);
            var bonusEntryInfo = Undo.AddComponent<AreaTransitionInfo>(bonusEntryTrigger.gameObject);
            var bonusEntrySo = new SerializedObject(bonusEntryInfo);
            bonusEntrySo.FindProperty("destinationArea").objectReferenceValue = bonusArea;
            bonusEntrySo.FindProperty("destinationPoint").objectReferenceValue = bonusSpawn;
            bonusEntrySo.ApplyModifiedProperties();
            WireTriggerInfo(bonusEntryTrigger, bonusEntryInfo);
            triggers.Add(bonusEntryTrigger);

            // 隠し部屋入口のヒント(法則F: 視覚的な違和感で気づかせる)。
            // 用途のない浮遊ブロックを頭上トリガーの下に置く。乗って跳べばトリガーに届く
            CreateHintBlock(main, 70.0f, 1.3f, roadMaterial, parent);

            // 隠し部屋の出口(部屋の奥) → 本筋へ戻る
            var bonusExitTrigger = CreateEventTrigger(
                "BonusExit", bonusSpline, 0.92f, new Vector3(6.0f, 4.0f, 0.5f), 0.0f, bonusContents.transform);
            var bonusExitInfo = Undo.AddComponent<AreaTransitionInfo>(bonusExitTrigger.gameObject);
            var bonusExitSo = new SerializedObject(bonusExitInfo);
            bonusExitSo.FindProperty("destinationArea").objectReferenceValue = area1;
            bonusExitSo.FindProperty("destinationPoint").objectReferenceValue = bonusReturn;
            bonusExitSo.ApplyModifiedProperties();
            WireTriggerInfo(bonusExitTrigger, bonusExitInfo);
            triggers.Add(bonusExitTrigger);

            // エリア2開始地点アンカー(ルート直下=エリア休眠の影響を受けない場所)
            var area2Start = CreateAnchor("Area2StartPoint", main2, TAtX(main2, 202.0f), rootTransform);

            // エリア1→2の遷移扉(エリア1末尾の扉からエリア2開始地点へ)
            var toArea2Trigger = CreateEventTrigger(
                "ToArea2", main, TAtX(main, 132.0f), new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent);
            var toArea2Info = Undo.AddComponent<AreaTransitionInfo>(toArea2Trigger.gameObject);
            var toArea2So = new SerializedObject(toArea2Info);
            toArea2So.FindProperty("destinationArea").objectReferenceValue = area2;
            toArea2So.FindProperty("destinationPoint").objectReferenceValue = area2Start;
            toArea2So.ApplyModifiedProperties();
            WireTriggerInfo(toArea2Trigger, toArea2Info);
            triggers.Add(toArea2Trigger);

            // 中間地点cp-02(エリア2に入った直後に必ず踏ませて、リスポン先をエリア2側へ更新する)
            var checkpoint2Trigger = CreateEventTrigger(
                "Checkpoint2", main2, TAtX(main2, 204.0f), new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent2);
            var checkpoint2Info = Undo.AddComponent<CheckPointInfo>(checkpoint2Trigger.gameObject);
            var checkpoint2So = new SerializedObject(checkpoint2Info);
            checkpoint2So.FindProperty("checkPointId").stringValue = "cp-02";
            checkpoint2So.FindProperty("respawnPoint").objectReferenceValue = area2Start;
            checkpoint2So.ApplyModifiedProperties();
            WireTriggerInfo(checkpoint2Trigger, checkpoint2Info);
            triggers.Add(checkpoint2Trigger);

            // ステージ出口(ゴールのさらに先。仮配置)
            var exitTrigger = CreateEventTrigger(
                "StageExit", main2, TAtX(main2, 304.0f), new Vector3(6.0f, 4.0f, 0.5f), 0.0f, parent2);
            WireTriggerInfo(exitTrigger, Undo.AddComponent<StageExitInfo>(exitTrigger.gameObject));
            triggers.Add(exitTrigger);

            // --- ステージ定義と配線 ---
            var definitionGo = new GameObject("StageDefinition");
            Undo.RegisterCreatedObjectUndo(definitionGo, "Create Stage1");
            definitionGo.transform.SetParent(rootTransform);
            var definition = Undo.AddComponent<StageDefinition>(definitionGo);

            var spawnPointGo = GameObject.Find("SpawnPoint");
            var spawnAnchor = spawnPointGo != null ? spawnPointGo.GetComponent<SplineAnchor>() : null;

            SetAreaStartPoint(area1, spawnAnchor);
            SetAreaStartPoint(bonusArea, bonusSpawn);
            SetAreaStartPoint(area2, area2Start);

            SetAreaEnemySpawnPoints(area1, area1EnemySpawns);
            SetAreaEnemySpawnPoints(area2, area2EnemySpawns);

            // エリア・トリガーの操作窓口はStageView。Definitionは初期エリアとViewの在り処だけ持つ
            var stageView = root != null ? root.GetComponent<StageView>() : null;
            WireStageView(stageView, new[] { area1, bonusArea, area2 }, triggers);

            var definitionSo = new SerializedObject(definition);
            definitionSo.FindProperty("initialArea").objectReferenceValue = area1;
            definitionSo.FindProperty("view").objectReferenceValue = stageView;
            definitionSo.ApplyModifiedProperties();

            // エリア2とボーナス部屋は初期休眠
            bonusContents.SetActive(false);
            area2Contents.SetActive(false);

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Stage1Creator] ステージ1を生成し、{ScenePath} に保存しました。");
        }

        static SplineContainer CreateSpline(string name, Vector3[] knots)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1 Spline");
            go.transform.position = Vector3.zero;

            var container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var knot in knots)
            {
                spline.Add(new BezierKnot((float3)knot), TangentMode.AutoSmooth);
            }

            return container;
        }

        /// <summary>
        /// StageAreaを作成し、中身のルートを配線する。
        /// </summary>
        static StageArea CreateArea(string name, GameObject contents, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1");
            go.transform.SetParent(parent);

            var area = Undo.AddComponent<StageArea>(go);
            var so = new SerializedObject(area);
            so.FindProperty("areaId").stringValue = name;
            so.FindProperty("contentsRoot").objectReferenceValue = contents;
            so.ApplyModifiedProperties();

            return area;
        }

        static void SetAreaStartPoint(StageArea area, SplineAnchor startPoint)
        {
            var so = new SerializedObject(area);
            so.FindProperty("startPoint").objectReferenceValue = startPoint;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Spline上の指定位置にSplineAnchorを作成する(スポーン・復帰地点用)。
        /// </summary>
        static SplineAnchor CreateAnchor(string name, SplineContainer target, float t, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1");
            go.transform.SetParent(parent);
            go.transform.position = (Vector3)target.EvaluatePosition(t);

            var anchor = Undo.AddComponent<SplineAnchor>(go);
            var so = new SerializedObject(anchor);
            so.FindProperty("splineContainer").objectReferenceValue = target;
            so.FindProperty("t").floatValue = Mathf.Clamp01(t);
            so.FindProperty("direction").enumValueIndex = (int)SplineAnchorDirection.Forward;
            so.ApplyModifiedProperties();

            return anchor;
        }

        /// <summary>
        /// Spline上の指定位置に、イベント用のトリガー(透明な箱)を置く。
        /// heightOffsetを上げると「ジャンプしないと触れない」トリガーになる。
        /// </summary>
        static AreaTrigger CreateEventTrigger(
            string name, SplineContainer target, float t,
            Vector3 size, float heightOffset, Transform parent)
        {
            Vector3 pos = (Vector3)target.EvaluatePosition(t);
            Vector3 tan = ((Vector3)target.EvaluateTangent(t));
            tan.y = 0.0f;
            Quaternion rot = tan.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(tan.normalized)
                : Quaternion.identity;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1");
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.up * heightOffset;
            go.transform.rotation = rot;

            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            box.size = size;
            box.center = new Vector3(0.0f, size.y * 0.5f, 0.0f);

            return Undo.AddComponent<AreaTrigger>(go);
        }

        /// <summary>
        /// AreaTriggerにイベント情報を配線する。
        /// </summary>
        static void WireTriggerInfo(AreaTrigger trigger, AreaEventInfo info)
        {
            var so = new SerializedObject(trigger);
            so.FindProperty("eventInfo").objectReferenceValue = info;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// StageViewへ全エリアと全トリガーを配線する。
        /// </summary>
        static void WireStageView(StageView view, IReadOnlyList<StageArea> areas, IReadOnlyList<AreaTrigger> triggers)
        {
            if (view == null)
            {
                Debug.LogError("[Stage1Creator] StageView not found on GeneratedStage.");
                return;
            }

            var so = new SerializedObject(view);

            var areasProperty = so.FindProperty("areas");
            areasProperty.arraySize = areas.Count;
            for (var i = 0; i < areas.Count; i++)
            {
                areasProperty.GetArrayElementAtIndex(i).objectReferenceValue = areas[i];
            }

            var triggersProperty = so.FindProperty("areaTriggers");
            triggersProperty.arraySize = triggers.Count;
            for (var i = 0; i < triggers.Count; i++)
            {
                triggersProperty.GetArrayElementAtIndex(i).objectReferenceValue = triggers[i];
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// x座標からSplineのt値を求める(xが単調なSpline用の二分探索)。
        /// 穴・トリガーの位置を設計座標(x)のまま指定するための逆引き。
        /// </summary>
        static float TAtX(SplineContainer container, float x)
        {
            float t0 = 0.0f;
            float t1 = 1.0f;
            bool increasing = container.EvaluatePosition(1.0f).x >= container.EvaluatePosition(0.0f).x;
            for (var i = 0; i < 40; i++)
            {
                float tm = (t0 + t1) * 0.5f;
                float xm = container.EvaluatePosition(tm).x;
                if ((xm < x) == increasing) t0 = tm; else t1 = tm;
            }
            return Mathf.Clamp01((t0 + t1) * 0.5f);
        }

        /// <summary>
        /// x座標をレール距離(railZ)へ変換する。
        /// 丘の勾配で弧長がx差より伸びるため、railZ指定のAPIへ渡す前に必ず変換する。
        /// </summary>
        static float RailZAtX(SplineContainer container, float x)
        {
            return container.Spline.ConvertIndexUnit(
                TAtX(container, x), PathIndexUnit.Normalized, PathIndexUnit.Distance);
        }

        /// <summary>
        /// x座標の範囲リストをレール距離(railZ)の範囲へ変換する(道の穴あけ用)。
        /// </summary>
        static Vector2[] HolesAtX(SplineContainer container, Vector2[] xRanges)
        {
            var result = new Vector2[xRanges.Length];
            for (var i = 0; i < xRanges.Length; i++)
            {
                result[i] = new Vector2(
                    RailZAtX(container, xRanges[i].x),
                    RailZAtX(container, xRanges[i].y));
            }
            return result;
        }

        static Vector2[] Concat(Vector2[] a, Vector2[] b)
        {
            var result = new Vector2[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }

        /// <summary>
        /// 隠し要素の位置を示す浮遊ブロックを置く(乗れる足場。Terrainレイヤー)。
        /// </summary>
        static void CreateHintBlock(
            SplineContainer target, float x, float height, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BonusHintBlock";
            Undo.RegisterCreatedObjectUndo(go, "Create Stage1");
            go.transform.SetParent(parent);

            Vector3 pos = (Vector3)target.EvaluatePosition(TAtX(target, x));
            go.transform.position = new Vector3(pos.x, height, pos.z);
            go.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
            go.layer = LayerMask.NameToLayer("Terrain");

            var renderer = go.GetComponent<Renderer>();
            if (material != null && renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Undo.AddComponent<GeneratedStageContent>(go);
        }

        /// <summary>
        /// 穴の範囲を前後に広げる(地面を道の穴より大きく裂いて谷に見せるため)。
        /// </summary>
        static Vector2[] PadHoles(Vector2[] holes, float padding)
        {
            var padded = new Vector2[holes.Length];
            for (var i = 0; i < holes.Length; i++)
            {
                padded[i] = new Vector2(holes[i].x - padding, holes[i].y + padding);
            }
            return padded;
        }

        /// <summary>
        /// Spline上に敵の出現地点(SplineAnchor + EnemySpawnPoint)を作成する。
        /// 敵の実体はシーンに置かず、実行時にStageAreaが生成する。
        /// </summary>
        static EnemySpawnPoint CreateEnemySpawnPoint(GameObject prefab, SplineContainer target, float t, Transform parent)
        {
            var go = new GameObject("EnemySpawnPoint");
            Undo.RegisterCreatedObjectUndo(go, "Create Enemy Spawn Point");
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = (Vector3)target.EvaluatePosition(t);

            var anchor = Undo.AddComponent<SplineAnchor>(go);
            var anchorSo = new SerializedObject(anchor);
            anchorSo.FindProperty("splineContainer").objectReferenceValue = target;
            anchorSo.FindProperty("t").floatValue = t;
            anchorSo.ApplyModifiedProperties();
            go.transform.rotation = anchor.GetRotation();

            var spawnPoint = Undo.AddComponent<EnemySpawnPoint>(go);
            var spawnSo = new SerializedObject(spawnPoint);
            spawnSo.FindProperty("prefab").objectReferenceValue = prefab.GetComponent<EnemyCharacter>();
            spawnSo.FindProperty("anchor").objectReferenceValue = anchor;
            spawnSo.ApplyModifiedProperties();

            Undo.AddComponent<GeneratedStageContent>(go);

            return spawnPoint;
        }

        static void SetAreaEnemySpawnPoints(StageArea area, IReadOnlyList<EnemySpawnPoint> spawnPoints)
        {
            var so = new SerializedObject(area);
            var property = so.FindProperty("enemySpawnPoints");
            property.arraySize = spawnPoints.Count;
            for (var i = 0; i < spawnPoints.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Enemy.prefabが無ければ、CharacterTestSceneのEnemyをプレハブ化して作る。
        /// </summary>
        static GameObject EnsureEnemyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (existing != null) return existing;

            if (!File.Exists(CharacterTestScenePath))
            {
                Debug.LogWarning($"[Stage1Creator] {CharacterTestScenePath} が見つかりません。");
                return null;
            }

            var scene = EditorSceneManager.OpenScene(CharacterTestScenePath, OpenSceneMode.Additive);
            GameObject prefab = null;

            try
            {
                GameObject enemySource = null;
                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var enemy = rootGo.GetComponentInChildren<EnemyCharacter>(true);
                    if (enemy != null)
                    {
                        enemySource = enemy.gameObject;
                        break;
                    }
                }

                if (enemySource == null)
                {
                    Debug.LogWarning("[Stage1Creator] CharacterTestSceneにEnemyCharacterが見つかりません。");
                    return null;
                }

                // 一時複製してシーン依存を切ってからプレハブ化
                var temp = Object.Instantiate(enemySource);
                temp.name = "Enemy";
                prefab = PrefabUtility.SaveAsPrefabAsset(temp, EnemyPrefabPath);
                Object.DestroyImmediate(temp);

                Debug.Log($"[Stage1Creator] {EnemyPrefabPath} を作成しました。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            return prefab;
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            Debug.Log("[Stage1Creator] 実行要求マーカーを検出しました。次のエディタ更新で実行します。");

            // InitializeOnLoad中のdelayCall登録は失われることがあるため、updateで自己解除方式にする
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

            EditorApplication.update -= RunOnce;
            File.Delete(RequestMarkerPath);

            try
            {
                CreateStage1();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
