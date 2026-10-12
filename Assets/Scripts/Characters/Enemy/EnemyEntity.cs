namespace Rail
{
    public sealed class EnemyEntity : CharacterEntity
    {
        public EnemyEntity(CharacterStatus status)
        {
            this.Hp = status.hp;
            this.MaxHp = status.hp;
            this.Attack = status.attack;
            this.Speed = status.speed;
            this.JumpForce = status.jumpForce;
        }
    }
}