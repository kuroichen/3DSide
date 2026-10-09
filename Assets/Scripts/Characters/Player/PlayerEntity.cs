namespace Rail
{
    public sealed class PlayerEntity : CharacterEntity, IPlayerEntity
    {
        public int Lives { get; internal set; }

        public PlayerEntity(
            int hp,
            int maxHp,
            int attack,
            int defense,
            int speed,
            int lives)
        {
            this.Hp = hp;
            this.MaxHp = maxHp;
            this.Attack = attack;
            this.Defense = defense;
            this.Speed = speed;
            this.Lives = lives;
        }
    }
}