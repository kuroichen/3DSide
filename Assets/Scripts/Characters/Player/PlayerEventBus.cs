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

        readonly Subject<Unit> onDeadSubject = new();
        readonly Subject<Unit> onDeathAnimationFinishedSubject = new();
        readonly Subject<Unit> onSpawnedSubject = new();

        public void Dispose()
        {
            this.onDeadSubject.Dispose();
            this.onDeathAnimationFinishedSubject.Dispose();
            this.onSpawnedSubject.Dispose();
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
    }
}