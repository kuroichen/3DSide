namespace Rail
{
    public enum DamageResult
    {
        Ignored, // 受け付けなかった（死亡済み, ダメージ0など）
        Damaged, // ダメージを受けたが生きている
        Died, // このダメージで死亡した
    }
}