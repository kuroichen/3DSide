using System;
using UniRx;

namespace Rail
{
    public interface IPlayerEventBus : IDisposable
    {
        IObservable<Unit> OnSpawnedObservable();
        IObservable<Unit> OnDeadObservable();
        IObservable<Unit> OnDeathAnimationFinishedObservable();
    }
}
