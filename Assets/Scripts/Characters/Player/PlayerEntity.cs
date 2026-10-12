namespace Rail
{
    public sealed class PlayerEntity : CharacterEntity, IPlayerEntity
    {
        public int Lives { get; internal set; }

        public PlayerEntity(
            int hp,
            int maxHp,
            int attack,
            float speed,
            float jumpForce,
            int lives)
        {
            this.Hp = hp;
            this.MaxHp = maxHp;
            this.Attack = attack;
            this.Speed = speed;
            this.JumpForce = jumpForce;
            this.Lives = lives;
        }
    }
}