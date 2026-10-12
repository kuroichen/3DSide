using System;
using UniRx;

namespace Rail
{
    public interface IPlayerEventBus : IDisposable
    {
        /// <summary>
        /// プレイヤー生成完了通知
        /// </summary>
        IObservable<Unit> OnSpawnedObservable();

        /// <summary>
        /// プレイヤー死亡通知
        /// </summary>
        IObservable<Unit> OnDeadObservable();

        /// <summary>
        /// プレイヤー死亡アニメーション完了通知
        /// </summary>
        IObservable<Unit> OnDeathAnimationFinishedObservable();

        /// <summary>
        /// プレイヤー被弾通知
        /// </summary>
        /// <returns></returns>
        IObservable<DamageInfo> OnAttackReceivedObservable();

        /// <summary>
        /// 残機を減らした通知
        /// </summary>
        IObservable<Unit> OnLifeConsumedObservable();

        /// <summary>
        /// 残機がなくなった通知
        /// </summary>
        IObservable<Unit> OnLivesDepletedObservable();
    }
}
