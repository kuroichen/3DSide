
namespace Rail
{
    public interface IStageEntity
    {
        string AreaId { get; }
        SplineAnchor SpawnPoint { get; }
    }
}