using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Helpers;
using Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer
{
    public class RoomResolver
    {
        private readonly Room _room;

        private readonly List<Transform> _tiles;
        private          RoomEnter       _enter;
        private readonly List<RoomExit>  _exits;

        private string TileTag => UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Tile);
        
        public RoomResolver(Room room)
        {
            _room  = room;
            _tiles = new List<Transform>();
            _exits = new List<RoomExit>();
        }

        public void Resolve()
        {
            if (_room == null)
                return;

            var grid = Grid.Default; 

            CollectChildrenData();
            var roomMatrix = BuildRoomMatrix(grid, out var bounds);
            _room.Bounds = BuildRoomBounds(bounds, grid);
            _room.Info   = BuildRoomInfo(roomMatrix, grid);
        }

        private HashSet<int3> BuildRoomMatrix(Grid grid, out (int minX, int minY, int minZ, int maxX, int maxY, int maxZ) bounds)
        {
            var roomMatrix = new HashSet<int3>();
            bounds = (int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, int.MinValue, int.MinValue);

            foreach (var tile in _tiles)
            {
                var tileGridIndex = grid.ToGrid(tile.position);

                if (tile.gameObject.activeInHierarchy)
                {
                    bounds.minX = math.min(bounds.minX, tileGridIndex.x);
                    bounds.maxX = math.max(bounds.maxX, tileGridIndex.x);
                    bounds.minY = math.min(bounds.minY, tileGridIndex.y);
                    bounds.maxY = math.max(bounds.maxY, tileGridIndex.y);
                    bounds.minZ = math.min(bounds.minZ, tileGridIndex.z);
                    bounds.maxZ = math.max(bounds.maxZ, tileGridIndex.z);
                }

                roomMatrix.Add(tileGridIndex);
            }

            return roomMatrix;
        }

        private Room.RoomBounds BuildRoomBounds((int minX, int minY, int minZ, int maxX, int maxY, int maxZ) bounds, Grid grid)
        {
            int3   centerIndex = grid.ToGrid(_room.transform.position);
            float3 worldMax    = grid.ToWorld(new int3(bounds.maxX, bounds.maxY, bounds.maxZ));
            float3 worldMin    = grid.ToWorld(new int3(bounds.minX, bounds.minY, bounds.minZ));

            var boundsWidth  = worldMax.x - worldMin.x + 1;
            var boundsHeight = worldMax.y - worldMin.y + 1;
            var boundsDepth  = worldMax.z - worldMin.z + 1;

            return new Room.RoomBounds
            {
                CenterIndex = centerIndex,
                Width       = boundsWidth,
                Height      = boundsHeight,
                Depth       = boundsDepth,
                Min         = new int3(bounds.minX, bounds.minY, bounds.minZ),
                Max         = new int3(bounds.maxX, bounds.maxY, bounds.maxZ)
            };
        }

        private Room.RoomInfo BuildRoomInfo(HashSet<int3> roomMatrix, Grid grid)
        {
            var hasEnter       = _enter != null;
            var enterGridIndex = new int3();
            var enterDirection = default(Room.RoomInfo.Direction);

            if (hasEnter)
            {
                enterGridIndex = grid.ToGrid(_enter.transform.position);
                enterDirection = ResolveDirection(roomMatrix, enterGridIndex);
            }

            var exitDirections = new Room.RoomInfo.Direction[_exits.Count];
            for (var i = 0; i < _exits.Count; i++)
            {
                var exit               = _exits[i];
                var tileLocalGridIndex = grid.ToGrid(exit.transform.position);
                exitDirections[i] = ResolveDirection(roomMatrix, tileLocalGridIndex);
            }

            var exitCount = _exits.Count(x => x != null);
            var exitGridIndices = exitCount > 0
                ? _exits.Select(x => grid.ToGrid(x.transform.position)).ToArray()
                : Array.Empty<int3>();

            return new Room.RoomInfo
            {
                HasEnter       = hasEnter,
                ExitCount      = exitCount,
                Enter          = enterGridIndex,
                EnterDirection = enterDirection,
                Exits          = exitGridIndices,
                ExitDirections = exitDirections,
                Tiles          = roomMatrix.ToArray()
            };
        }

        private void CollectChildrenData()
        {
            _tiles.Clear();
            _exits.Clear();
            _enter = null;

            foreach (Transform child in _room.transform.GetChildren(1))
            {
                if (child.CompareTag(TileTag))
                    _tiles.Add(child);

                if (child.TryGetComponent(out RoomEnter enter))
                    _enter = enter;

                if (child.TryGetComponent(out RoomExit exit))
                    _exits.Add(exit);
            }
        }

        private static int3 ToLocalGridIndex(Transform transform, float4x4 trs, Grid grid)
        {
            var localPosition = math.mul(trs, new float4(transform.localPosition, 1)).xyz;
            return grid.ToGrid(localPosition);
        }

        private static Room.RoomInfo.Direction ResolveDirection(HashSet<int3> roomMatrix, int3 tileLocalGridIndex)
        {
            var forwardTile  = tileLocalGridIndex + new int3(0,  0, 1);
            var rightTile    = tileLocalGridIndex + new int3(1,  0, 0);
            var backwardTile = tileLocalGridIndex + new int3(0,  0, -1);
            var leftTile     = tileLocalGridIndex + new int3(-1, 0, 0);

            if (!roomMatrix.Contains(forwardTile))
                return Room.RoomInfo.Direction.Forward;
            if (!roomMatrix.Contains(backwardTile))
                return Room.RoomInfo.Direction.Backward;
            if (!roomMatrix.Contains(leftTile))
                return Room.RoomInfo.Direction.Left;
            if (!roomMatrix.Contains(rightTile))
                return Room.RoomInfo.Direction.Right;

            return default;
        }
    }
}