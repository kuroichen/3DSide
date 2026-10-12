
namespace Rail
{
    /// <summary>
    /// アイテムの効果。
    /// </summary>
    public readonly struct ItemEffect
    {
        public ItemEffectType Type { get; }
        public int Amount { get; }

        public ItemEffect(ItemEffectType type, int amount)
        {
            this.Type = type;
            this.Amount = amount;
        }
    }
}