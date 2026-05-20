using Unity.Mathematics;

namespace Extensions
{
    public static class Bool3x3Extension
    {
        public enum RotationSteps
        {
            None         = 0,
            Clockwise90  = 1,
            Clockwise180 = 2,
            Clockwise270 = 3
        }

        public static bool3x3 Rotate(this bool3x3 m, RotationSteps steps)
        {
            return steps switch
            {
                RotationSteps.Clockwise90  => new bool3x3(
                    new bool3(m.c0.z, m.c1.z, m.c2.z),
                    new bool3(m.c0.y, m.c1.y, m.c2.y),
                    new bool3(m.c0.x, m.c1.x, m.c2.x)),

                RotationSteps.Clockwise180 => new bool3x3(
                    new bool3(m.c2.z, m.c2.y, m.c2.x),
                    new bool3(m.c1.z, m.c1.y, m.c1.x),
                    new bool3(m.c0.z, m.c0.y, m.c0.x)),

                RotationSteps.Clockwise270 => new bool3x3(
                    new bool3(m.c2.x, m.c1.x, m.c0.x),
                    new bool3(m.c2.y, m.c1.y, m.c0.y),
                    new bool3(m.c2.z, m.c1.z, m.c0.z)),

                _ => m // None or default
            };
        }
    }
}