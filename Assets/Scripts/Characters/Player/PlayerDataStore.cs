using System;
using UniRx;

namespace Rail
{
    public class PlayerDataStore : IDisposable
    {
        public IPlayerEntity Entity => this.entity;
        public IObservable<int> OnHpChangedObservable() => this.onHpChangedSubject;
        public IObservable<int> OnLivesChangedObservable() => this.onLivesChangedSubject;

        readonly Subject<int> onHpChangedSubject = new();
        readonly Subject<int> onLivesChangedSubject = new();

        readonly PlayerEntity entity = null;
        

        public PlayerDataStore(PlayerEntity entity)
        {
            this.entity = entity;
        }

        void IDisposable.Dispose()
        {
            this.onHpChangedSubject.Dispose();
            this.onLivesChangedSubject.Dispose();
        }

        /// <summary>
        /// HPを更新し、値が変わった場合に通知する。
        /// </summary>
        /// <param name="hp"></param>
        public void SetHp(int hp)
        {
            if (this.entity.Hp == hp) return;

            this.entity.Hp = hp;
            this.onHpChangedSubject.OnNext(this.entity.Hp);
        }

        /// <summary>
        /// 残機を更新し、値が変わった場合に通知する。
        /// </summary>
        /// <param name="lives"></param>
        public void SetLives(int lives)
        {
            if (this.entity.Lives == lives) return;

            this.entity.Lives = lives;
            this.onLivesChangedSubject.OnNext(this.entity.Lives);
        }
    }
}
