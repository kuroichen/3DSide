namespace Rail
{
    public class HealItem : ItemBase
    {
        const int HEAL_AMOUNT = 1;

        protected override ItemEffect Effect => new (ItemEffectType.Heal, HEAL_AMOUNT);
    }
}
