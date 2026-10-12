namespace Rail
{
    public abstract class CharacterEntity : ICharacterEntity
    {
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }

        public int Attack { get; internal set; }

        public float Speed { get; internal set; }
        public float JumpForce { get; internal set; }
    }
}