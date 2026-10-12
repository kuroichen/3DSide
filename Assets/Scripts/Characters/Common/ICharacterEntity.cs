namespace Rail
{
    public interface ICharacterEntity
    {
        int Hp { get; }
        int MaxHp { get; }

        int Attack { get; }

        float Speed { get; }
        float JumpForce { get; }
    }
}