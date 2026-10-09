using VContainer;
using System;
using NUnit.Framework;

namespace Rail
{
    public sealed class StageModel : IDisposable
    {
        readonly StageDataStore dataStore = null;

        public IStageEntity Entity => this.dataStore.Entity;

        [Inject]
        public StageModel(
            StageDataStore dataStore)
        {
            this.dataStore = dataStore;
        }

        void IDisposable.Dispose()
        {
            
        }

        public bool TryChangeArea(string areaId, SplineAnchor destinationPoint)
        {
            if (string.IsNullOrEmpty(areaId) || destinationPoint == null || this.Entity?.AreaId == areaId)
            {
                return false;
            }

            this.dataStore.SetEntity(new StageEntity(areaId, destinationPoint));

            return true;
        }
    }
}