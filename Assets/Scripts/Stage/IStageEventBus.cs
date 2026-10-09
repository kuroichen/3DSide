using System;

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
    }
}
