using UnityEngine;

namespace Rail
{
    public interface ICharacterProperties
    {
        ILife LifeProperty { get; }
        IAttack AttackProperty { get; }
    }
}
