using System;
using UniRx;

namespace Rail
{
    public interface ILife
    {
        int MaxHp { get; }
        int CurrentHp { get; }
        IObservable<Unit> OnDeadObservable();
        IObservable<int> OnLifeChangedObservable();
        IObservable<int> OnAddLifeObservable();
        IObservable<int> OnSubLifeObservable();

        void Heal(int amount);
        void TakeDamage(int damage);
    }
}
