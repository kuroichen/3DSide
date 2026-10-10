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
        
        public IObservable<SplineAnchor> OnRespawnRequestObservable() => this.onRespawnRequestSubject;

        public IObservable<Unit> OnGameOverObservable() => this.onGameOverSubject;

        readonly Subject<AreaEventInfo> onAreaTriggerSubject = new();
        readonly Subject<SplineAnchor> onPreparedSubject = new();
        readonly Subject<SplineAnchor> onRespawnRequestSubject = new();
        readonly Subject<Unit> onGameOverSubject = new();


        void IDisposable.Dispose()
        {
            this.onAreaTriggerSubject.Dispose();
            this.onPreparedSubject.Dispose();
            this.onRespawnRequestSubject.Dispose();
            this.onGameOverSubject.Dispose();
        }

        public void PublishAreaTrigger(AreaEventInfo areaEventInfo)
        {
            this.onAreaTriggerSubject.OnNext(areaEventInfo);
        }

        public void PublishPrepared(SplineAnchor splineAnchor)
        {
            this.onPreparedSubject.OnNext(splineAnchor);
        }

        public void PublishRespawnRequest(SplineAnchor splineAnchor)
        {
            this.onRespawnRequestSubject.OnNext(splineAnchor);
        }

        public void PublishGameOver()
        {
            this.onGameOverSubject.OnNext(Unit.Default);
        }
    }
}
