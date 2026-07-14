using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rail
{
    /// <summary>
    /// ステージの見た目(テーマ)の定義。
    /// 道のマテリアル・道端の飾りの散布・空とフォグをデータとして持ち、
    /// ステージ生成ツールがこれを読んで見た目を適用する。
    /// </summary>
    [CreateAssetMenu(menuName = "Rail/Stage Theme")]
    public class StageTheme : ScriptableObject
    {
        public enum ScatterSide
        {
            BothSides, // 道の両脇
            FarSide,   // カメラの反対側(奥)だけ。背の高い物で手前の視界を塞がないため
        }

        [Serializable]
        public class ScatterEntry
        {
            public string label = "";
            public GameObject[] prefabs = null;   // 候補からランダムに選ぶ
            public float densityPer10m = 1.0f;    // 道10mあたりの個数
            public float minSideOffset = 2.1f;    // レール中心からの横距離
            public float maxSideOffset = 2.8f;    // (プレイヤー可動域±2mの外、道幅3mの内が飾り帯)
            public float minScale = 0.8f;
            public float maxScale = 1.3f;
            public ScatterSide side = ScatterSide.BothSides;
        }

        [Header("道")]
        public Material roadMaterial = null;

        [Header("散布(道の縁の飾り)")]
        public List<ScatterEntry> scatter = new();

        [Header("環境")]
        public Material skyboxMaterial = null;
        public bool enableFog = true;
        public Color fogColor = new Color(0.75f, 0.85f, 0.95f);
        public float fogDensity = 0.012f;
        public Color sunColor = new Color(1.0f, 0.96f, 0.9f);
        public Vector3 sunEuler = new Vector3(50.0f, -30.0f, 0.0f);

        [Header("乱数シード(再生成しても配置が変わらないように固定)")]
        public int seed = 12345;
    }
}
