using System;
using UniRx;

namespace Rail
{
    public class PlayerEventBus : IPlayerEventBus
    {
        /// <summary>
        /// 死亡通知
        /// </summary>
        public IObservable<Unit> OnDeadObservable() => this.onDeadSubject;

        /// <summary>
        /// 死亡アニメーション終了通知
        /// </summary>
        public IObservable<Unit> OnDeathAnimationFinishedObservable() => this.onDeathAnimationFinishedSubject;

        /// <summary>
        /// スポーン通知
        /// </summary>
        public IObservable<Unit> OnSpawnedObservable() => this.onSpawnedSubject;

        /// <summary>
        /// 残機を減らした通知
        /// </summary>
        public IObservable<Unit> OnLifeConsumedObservable() => this.onLifeConsumedSubject;

        /// <summary>
        /// 残機がなくなった通知
        /// </summary>
        public IObservable<Unit> OnLivesDepletedObservable() => this.onLivesDepletedSubject;

        readonly Subject<Unit> onDeadSubject = new();
        readonly Subject<Unit> onDeathAnimationFinishedSubject = new();
        readonly Subject<Unit> onSpawnedSubject = new();
        readonly Subject<Unit> onLifeConsumedSubject = new();
        readonly Subject<Unit> onLivesDepletedSubject = new();

        public void Dispose()
        {
            this.onDeadSubject.Dispose();
            this.onDeathAnimationFinishedSubject.Dispose();
            this.onSpawnedSubject.Dispose();
            this.onLifeConsumedSubject.Dispose();
            this.onLivesDepletedSubject.Dispose();
        }

        /// <summary>
        /// 死亡を通知
        /// </summary>
        public void PublishDead()
        {
            this.onDeadSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 死亡アニメーション完了を通知
        /// </summary>
        public void PublishDeathAnimationFinished()
        {
            this.onDeathAnimationFinishedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// スポーンを通知
        /// </summary>
        public void PublishSpawned()
        {
            this.onSpawnedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 残機が減ったことを通知
        /// </summary>
        public void PublishLifeConsumed()
        {
            this.onLifeConsumedSubject.OnNext(Unit.Default);
        }

        /// <summary>
        /// 残機がなくなったことを通知
        /// </summary>
        public void PublishLivesDepleted()
        {
            this.onLivesDepletedSubject.OnNext(Unit.Default);
        }
    }
}