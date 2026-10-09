namespace Rail
{
    public sealed class StageEntity : IStageEntity
    {     
        public string AreaId { get; internal set; }

        public SplineAnchor SpawnPoint { get; internal set; }

        public StageEntity(
            string areaId,
            SplineAnchor spawnPoint)
        {
            this.AreaId = areaId;
            this.SpawnPoint = spawnPoint;
        }
    }
}