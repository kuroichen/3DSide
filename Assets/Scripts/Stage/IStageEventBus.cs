using System;
using UniRx;

namespace Rail
{
    public interface IStageEventBus : IDisposable
    {
        /// <summary>
        /// エリアイベント通知
        /// </summary>
        IObservable<AreaEventInfo> OnAreaTriggerObservable();

        /// <summary>
        /// エリア準備完了通知
        /// </summary>
        IObservable<SplineAnchor> OnPreparedObservable();

        /// <summary>
        /// リスポーン指示通知
        /// </summary>
        IObservable<SplineAnchor> OnRespawnRequestObservable();

        /// <summary>
        /// ゲームオーバー通知
        /// </summary>
        IObservable<Unit> OnGameOverObservable();
    }
}
