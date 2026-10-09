using System;

namespace Rail
{
    public class PlayerModel : IDisposable
    {
        PlayerDataStore dataStore = null;

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


        void Damage(int value)
        {
            var hp = this.dataStore.Entity.Hp - value;
            hp = Math.Max(0, hp);

            this.dataStore.SetHp(hp);
        }

        void Heal(int value)
        {
            var hp = this.dataStore.Entity.Hp + value;
            hp = Math.Min(this.dataStore.Entity.MaxHp, hp);

            this.dataStore.SetHp(hp);
        }

        void Reset()
        {
            this.dataStore.SetHp(this.dataStore.Entity.MaxHp);
        }
    }
}
