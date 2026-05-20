using Mechanics.World_Designer;
using Unity.Mathematics;

public static class AlignmentUtility
{
    public static int3 CalculateOffset(RoomScriptableObject.BoundsInfo bounds, int3 point1, int3 point2)
    {
        return point2 - point1;
    }

    public static RuntimeDescriptor.AlignmentData CalculateAlignment(RoomScriptableObject.BoundsInfo bounds, int3 point1, int3 point2, int rotation = 0)
    {
        return new RuntimeDescriptor.AlignmentData
        {
            Offset   = CalculateOffset(bounds, point1, point2),
            Rotation = rotation
        };
    }
}