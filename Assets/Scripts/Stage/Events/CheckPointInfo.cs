using UnityEngine;

namespace Rail
{
    /// <summary>
    /// AreaTriggerで発火するイベント用の情報を保持するクラス
    /// 中間地点、復帰に必要な情報を保持する
    /// </summary>
    public class CheckPointInfo : AreaEventInfo
    {
        [SerializeField, Tooltip("チェックポイントの識別しやすいID")]
        string checkPointId = "";

        [SerializeField, Tooltip("チェックポイントごとのリスポーン位置")]
        SplineAnchor respawnPoint = null;

        public string CheckPointId => this.checkPointId;
        public SplineAnchor RespawnPoint => this.respawnPoint;
    }
}