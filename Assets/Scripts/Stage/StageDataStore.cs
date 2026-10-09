
using System;
using UniRx;

namespace Rail
{
    public sealed class StageDataStore : IDisposable
    {
        public IStageEntity Entity => this.entity;
        public IObservable<IStageEntity> OnAreaChangedObservable() => this.onAreaChangedSubject;

        readonly Subject<IStageEntity> onAreaChangedSubject = new();

        IStageEntity entity = null;

        public StageDataStore()
        {
        }

        void IDisposable.Dispose()
        {
            this.onAreaChangedSubject.Dispose();
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
    }
}