namespace Rail
{
    public interface ICharacterEntity
    {
        int Hp { get; }
        int MaxHp { get; }

        int Attack { get; }
        int Defense { get; }

        int Speed { get; }
    }
}