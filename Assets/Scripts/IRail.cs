using UnityEngine;

namespace Rail
{
    public interface IRail
    {
        float Length { get; }
        Vector3 Local2World(Vector3 localPosition);
        Vector3? World2Local(Vector3 worldPosition);
        Quaternion Rotation(float railZ);
    }
}
