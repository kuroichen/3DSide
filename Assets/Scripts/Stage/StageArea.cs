using System;
using UnityEngine;

namespace Rail
{
    /// <summary>
    /// ステージ内の各エリアを管理するクラス。
    /// </summary>
    public class StageArea : MonoBehaviour
    {
        [SerializeField]
        string areaId = "";

        [SerializeField]
        GameObject contentsRoot = null;

        [SerializeField]
        SplineAnchor startPoint = null;

        // 
        public string AreaId => this.areaId;

        // このエリアのスタート地点
        public SplineAnchor StartPoint => this.startPoint;
    
        public void SetAreaActive(bool active)
        {
            if (this.contentsRoot == null)
            {
                Debug.LogWarning($"[{name}] ContentsRoot is not assigned.", this);
                return;
            }

            this.contentsRoot.SetActive(active);
        }
    }
}