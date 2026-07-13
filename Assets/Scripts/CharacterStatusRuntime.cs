using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class CharacterStatusRuntime : ILife, IAttack, IDisposable
    {
        int hp;
        int maxHp;
        int attack;

        float speed;
        public float Speed => this.speed;
        float jumpForce;
        public float JumpForce => this.jumpForce;


        public CharacterStatusRuntime(CharacterStatus status)
        {
            this.hp = status.hp;
            this.maxHp = status.hp;
            this.speed = status.speed;
            this.jumpForce = status.jumpForce;
            this.attack = status.attack;
        }

        void IDisposable.Dispose()
        {
            this.onSubLifeSubject.Dispose();
            this.onAddLifeSubject.Dispose();
            this.onLifeChangedSubject.Dispose();
            this.onDeadSubject.Dispose();
        }

        #region Life関連

        public int MaxHp => this.maxHp;
        public int CurrentHp => this.hp;

        Subject<Unit> onDeadSubject = new();
        Subject<int> onLifeChangedSubject = new();
        Subject<int> onAddLifeSubject = new();
        Subject<int> onSubLifeSubject = new();

        public IObservable<Unit> OnDeadObservable() => this.onDeadSubject;
        public IObservable<int> OnLifeChangedObservable() => this.onLifeChangedSubject;
        public IObservable<int> OnAddLifeObservable() => this.onAddLifeSubject;
        public IObservable<int> OnSubLifeObservable() => this.onSubLifeSubject;

        /// <summary>
        /// ダメージ付与
        /// </summary>
        public void TakeDamage(int damage)
        {
            this.hp = Mathf.Max(0, this.CurrentHp - damage);

            this.onSubLifeSubject.OnNext(damage);
            this.onLifeChangedSubject.OnNext(this.hp);
            
            if (this.hp <= 0)
            {
                this.onDeadSubject.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// 回復
        /// </summary>
        public void Heal(int amount)
        {
            var healed = Mathf.Min(amount, this.MaxHp - this.hp);
            this.hp += healed;

            this.onAddLifeSubject.OnNext(healed);
            this.onLifeChangedSubject.OnNext(this.hp);
        }

        #endregion

        #region Attack関連

        public int Attack => this.attack;

        #endregion
    }
}
