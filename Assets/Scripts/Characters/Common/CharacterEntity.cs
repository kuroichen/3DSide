namespace Rail
{
    public abstract class CharacterEntity : ICharacterEntity
    {
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }

        public int Attack { get; internal set; }
        public int Defense { get; internal set; }

        public int Speed { get; internal set; }
    }
}