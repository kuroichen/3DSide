
using System;
using UniRx;

namespace Rail
{
    public sealed class StageDataStore : IDisposable
    {
        public IStageEntity Entity => this.entity;
        public StagePhase Phase => this.phase;

        public IObservable<IStageEntity> OnAreaChangedObservable() => this.onAreaChangedSubject;
        public IObservable<StagePhase> OnPhaseChangedObservable() => this.onPhaseChangedSubject;

        readonly Subject<IStageEntity> onAreaChangedSubject = new();
        readonly Subject<StagePhase> onPhaseChangedSubject = new();

        IStageEntity entity = null;
        StagePhase phase = StagePhase.Loading;

        public StageDataStore()
        {
        }

        void IDisposable.Dispose()
        {
            this.onAreaChangedSubject.Dispose();
            this.onPhaseChangedSubject.Dispose();
        }

        public void SetEntity(IStageEntity entity)
        {
            if (entity == null || ReferenceEquals(this.entity, entity))
            {
                return;
            }

            this.entity = entity;
            this.onAreaChangedSubject.OnNext(this.entity);
        }

        public void SetPhase(StagePhase phase)
        {
            if (this.phase == phase) return;

            this.phase = phase;
            this.onPhaseChangedSubject.OnNext(this.phase);
        }
    }
}