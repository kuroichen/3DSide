using System;

namespace Rail
{
    public class PlayerModel : IDisposable
    {
        public IObservable<int> OnHpChangedObservable() => this.dataStore.OnHpChangedObservable();
        public IObservable<int> OnLivesChangedObservable() => this.dataStore.OnLivesChangedObservable();

        public IPlayerEntity Entity => this.dataStore.Entity;

        readonly PlayerDataStore dataStore = null;

        public PlayerModel(
            PlayerDataStore dataStore)
        {
            this.dataStore = dataStore;
        }

        void IDisposable.Dispose()
        {
            
        }

        /// <summary>
        /// 残機を一つ消費できた場合はtrueを返す。
        /// </summary>
        /// <returns>bool 残機消費ができた場合にtrue</returns>
        public bool TryConsumeLife()
        {
            var lives = this.dataStore.Entity.Lives;

            if (lives <= 0) return false;

            this.dataStore.SetLives(lives - 1);
            return true;
        }

        /// <summary>
        /// ダメージを受け、その結果を返す。
        /// </summary>
        public DamageResult Damage(int damage)
        {
            var hp = this.dataStore.Entity.Hp;

            if (damage <= 0 || hp <= 0)
            {
                return DamageResult.Ignored; 
            }

            this.dataStore.SetHp(Math.Max(0, hp - damage));

            return this.dataStore.Entity.Hp <= 0
                ? DamageResult.Died
                : DamageResult.Damaged;
        }

        /// <summary>
        /// HPを最大値に戻す。プレイヤーの生成時に呼ぶ。
        /// </summary>
        public void ResetHp()
        {
            this.dataStore.SetHp(this.dataStore.Entity.MaxHp);
        }
    }
}
