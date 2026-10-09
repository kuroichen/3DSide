
namespace Rail
{
    public interface IPlayerInputSource
    {
        float Move { get; }
        bool JumpPressed { get; }
        bool JumpHeld { get; }
        bool AttackPressed { get; }
    }
}