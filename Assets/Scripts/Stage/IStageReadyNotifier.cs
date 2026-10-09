using System;

namespace Rail
{
    public interface IStageReadyNotifier
    {
        IObservable<SplineAnchor> OnPreparedObservable();
    }
}