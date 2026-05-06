using Unity.Mathematics;

namespace Mechanics.World_Designer
{
    public class Grid
    {
        private float3 _tileSize;
        private float3 _gridOffset;

        public float3 TileSize => _tileSize;

        public float3 GridOffset => _gridOffset;

        /// <summary>
        /// Creates a world aligned grid
        /// <returns>
        /// Grid with default GridOffset (0.0f, 0.0f, 0.0f) and TileSize (1.0f, 1.0f, 1.0f)
        /// </returns>
        /// </summary>
        public Grid()
        {
            _tileSize   = new float3(1.0f, 1.0f, 1.0f);
            _gridOffset = new float3(0.0f, 0.0f, 0.0f);
        }

        public Grid WithTileSize(float3 tileSize)
        {
            _tileSize = tileSize;
            return this;
        }

        public Grid WithGridOffset(float3 gridOffset)
        {
            _gridOffset = gridOffset;
            return this;
        }

        public int3 ToGrid(float3 point)
        {
            float3 precisionOffset = new float3(0.0001f, 0.0001f, 0.0001f);
            return (int3)math.floor((point + precisionOffset - GridOffset) / TileSize);
        }

        public float3 ToWorld(int3 gridIndex)
        {
            float3 localPos = (new float3(gridIndex.x, gridIndex.y, gridIndex.z) * TileSize);
            return localPos + GridOffset;
        }
    }
}