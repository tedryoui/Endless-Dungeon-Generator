using Unity.Mathematics;

namespace Mechanics.World_Designer
{
    public class Grid
    {
        public enum GridMode { World, Local }

        private float3 _tileSize;
        private GridMode _mode;

        private float3 _origin;
        private quaternion _rotation;
        private float3x3 _rotationMatrix;
        private float3x3 _inverseRotationMatrix;

        public float3 TileSize => _tileSize;
        public GridMode Mode => _mode;
        public float3 Origin => _origin;

        /// <summary>
        /// Создвет стандартную сетку
        /// World типа с origin в (0.5f, 0.0f, 0.5f)
        /// </summary>
        public static Grid Default
        {
            get
            {
                var grid = Create(new float3(1), Grid.GridMode.World, new float3(0.5f, 0.0f, 0.5f));
                return grid;
            }
        }

        private Grid()
        {
            
        }

        /// <summary>
        /// Создаёт сетку.
        /// World — начало в (0,0,0), без поворота.
        /// Local — задаётся origin и rotation (например, трансформ комнаты).
        /// </summary>
        public static Grid Create(
            float3 tileSize,
            GridMode mode = GridMode.World,
            float3 origin = default,
            quaternion rotation = default)
        {
            if (math.all(rotation.value == float4.zero))
                rotation = quaternion.identity;

            var grid = new Grid
            {
                _tileSize = tileSize,
                _mode = mode,
                _origin = origin,
                _rotation = rotation,
                _rotationMatrix = math.float3x3(rotation),
            };

            grid._inverseRotationMatrix = math.transpose(grid._rotationMatrix);

            return grid;
        }

        /// <summary>
        /// Переводит мировую позицию в индекс ячейки сетки.
        /// </summary>
        public int3 ToGrid(float3 worldPosition)
        {
            float3 local = worldPosition - _origin;

            if (_mode == GridMode.Local)
                local = math.mul(_inverseRotationMatrix, local);

            return new int3(
                (int)math.floor(local.x / _tileSize.x),
                (int)math.floor(local.y / _tileSize.y),
                (int)math.floor(local.z / _tileSize.z)
            );
        }

        /// <summary>
        /// Переводит индекс ячейки в мировую позицию (центр ячейки).
        /// </summary>
        public float3 ToWorld(int3 index)
        {
            // Центр ячейки в локальном пространстве сетки
            float3 local = new float3(
                (index.x + 0.5f) * _tileSize.x,
                (index.y + 0.5f) * _tileSize.y,
                (index.z + 0.5f) * _tileSize.z
            );

            if (_mode == GridMode.Local)
                local = math.mul(_rotationMatrix, local);

            return _origin + local;
        }

        /// <summary>
        /// Создаёт дочернюю локальную сетку относительно текущей.
        /// Удобно для иерархий: Room->Tile, Building->Room->Tile.
        /// </summary>
        public Grid CreateChild(int3 parentIndex, float3 childTileSize, quaternion localRotation = default)
        {
            float3 childOrigin = ToWorld(parentIndex) - _tileSize * 0.5f;

            if (math.all(localRotation.value == float4.zero))
                localRotation = quaternion.identity;

            quaternion combinedRotation = math.mul(_rotation, localRotation);

            return Create(childTileSize, GridMode.Local, childOrigin, combinedRotation);
        }
    }
}