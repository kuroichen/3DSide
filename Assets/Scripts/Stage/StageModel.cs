using VContainer;
using System;

namespace Rail
{
    public sealed class StageModel : IDisposable
    {
        readonly StageDataStore dataStore = null;

        public IStageEntity Entity => this.dataStore.Entity;
        public StagePhase Phase => this.dataStore.Phase;

        public IObservable<StagePhase> OnPhaseChangedObservable() => this.dataStore.OnPhaseChangedObservable();

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

        /// <summary>
        /// 進行状態を遷移させる
        /// </summary>
        public bool TryChangePhase(StagePhase nextPhase)
        {
            if (!CanChangePhase(this.Phase, nextPhase)) return false;

            this.dataStore.SetPhase(nextPhase);
            return true;
        }

        static bool CanChangePhase(StagePhase current, StagePhase next) => (current, next) switch
        {
            (StagePhase.Loading, StagePhase.Playing) => true,
            (StagePhase.Playing, StagePhase.Cleared) => true,
            (StagePhase.Playing, StagePhase.Exiting) => true,
            _ => false
        };
    }
}