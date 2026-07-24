using UnityEngine;

namespace Rail
{
    public class HealItem : ItemBase
    {
        const int HEAL_AMOUNT = 1;
        protected override void OnCollected(ICharacterProperties characterProperties)
        {
            characterProperties.LifeProperty.Heal(HEAL_AMOUNT);

            Destroy(this.gameObject);         
        }   
    }
}
