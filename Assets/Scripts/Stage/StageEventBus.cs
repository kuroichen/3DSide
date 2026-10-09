using System;
using UniRx;

namespace Rail
{
    /// <summary>
    /// Stage側イベントを仲介するクラス。
    /// </summary>
    public sealed class StageEventBus : IStageEventBus
    {
        public IObservable<AreaEventInfo> OnAreaTriggerObservable() => this.onAreaTriggerSubject;

        public IObservable<SplineAnchor> OnPreparedObservable() => this.onPreparedSubject;

        readonly Subject<AreaEventInfo> onAreaTriggerSubject = new();
        readonly Subject<SplineAnchor> onPreparedSubject = new();

        void IDisposable.Dispose()
        {
            this.onAreaTriggerSubject.Dispose();
            this.onPreparedSubject.Dispose();
        }

        public void PublishAreaTrigger(AreaEventInfo areaEventInfo)
        {
            this.onAreaTriggerSubject.OnNext(areaEventInfo);
        }

        public void PublishPrepared(SplineAnchor splineAnchor)
        {
            this.onPreparedSubject.OnNext(splineAnchor);
        }
    }
}
