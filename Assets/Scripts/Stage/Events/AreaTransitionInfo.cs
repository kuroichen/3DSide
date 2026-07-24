using UnityEngine;

namespace Rail
{
    /// <summary>
    /// AreaTriggerで発火するイベント用の情報を保持するクラス
    /// エリア切り替えに必要な情報を保持する
    /// </summary>
    public class AreaTransitionInfo : AreaEventInfo
    {
        [SerializeField, Tooltip("遷移先のエリア")]
        StageArea destinationArea = null;

        [SerializeField, Tooltip("遷移先のスポーンポイント")]
        SplineAnchor destinationPoint = null;

        public StageArea DestinationArea => this.destinationArea;
        public SplineAnchor DestinationPoint => this.destinationPoint;
    }
}