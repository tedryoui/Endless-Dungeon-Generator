using Unity.Mathematics;

namespace Extensions
{
    public static class VectorExtension
    {
        /// <summary>
        /// Поворачивает точку вокруг заданного центра (pivot) используя углы Эйлера (в радианах).
        /// </summary>
        /// <param name="point">Исходная точка</param>
        /// <param name="pivot">Центр вращения</param>
        /// <param name="eulerDegrees">Углы поворота в градусах (float3)</param>
        public static float3 RotateAroundXYZ(this float3 point, float3 pivot, float3 eulerDegrees)
        {
            quaternion rotation = quaternion.Euler(math.radians(eulerDegrees));
            
            return math.mul(rotation, (point - pivot)) + pivot;
        }
    }
}