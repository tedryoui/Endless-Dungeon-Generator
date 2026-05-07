using System;
using System.Linq;
using Attributes;
using Extensions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Mechanics.World_Designer.Gizmos
{
    [ExecuteInEditMode, ExecuteAlways]
    public class RoomGizmoDrawer : MonoBehaviour
    {
        [SerializeField, DisabledProperty] private Room _room;
        
        private void OnEnable()
        {
            _room ??= GetComponent<Room>();
        }

        private void OnDrawGizmos()
        {
            DrawWireBounds();
            DrawTileBounds();
            DrawEnterTile();
            DrawExitTiles();
            DrawAreaTiles();
        }

        private void DrawEnterTile()
        {
            var roomInfo   = _room.Info;
            var grid       = Grid.Default;
            var enter      = roomInfo.Enter;
            var enterWorld = grid.ToWorld(enter);

            UnityEngine.Gizmos.color = Color.green;
            UnityEngine.Gizmos.DrawCube(enterWorld, Vector3.one * 0.85f);
        }

        private void DrawExitTiles()
        {
            var roomBounds = _room.Bounds;
            var roomInfo   = _room.Info;
            var grid       = Grid.Default;
            var exits      =  roomInfo.Exits;

            foreach (var exit in exits)
            {
                var exitWorld = grid.ToWorld(exit);
                
                UnityEngine.Gizmos.color = Color.red;
                UnityEngine.Gizmos.DrawCube(exitWorld, Vector3.one * 0.75f);
            }
        }

        private void DrawWireBounds()
        {
            var roomBounds   = _room.Bounds;
            var grid         =  Grid.Default;
            var centerTile   = roomBounds.CenterIndex;
            var boundsCenter = _room.transform.GetSceneCenter();
            var size         = new Vector3(roomBounds.Width, 1.0f, roomBounds.Depth);

            UnityEngine.Gizmos.color = Color.gray2;
            UnityEngine.Gizmos.DrawSphere(grid.ToWorld(centerTile), 0.25f);
            
            UnityEngine.Gizmos.color = Color.black;
            UnityEngine.Gizmos.DrawSphere(boundsCenter, 0.125f);
            
            UnityEngine.Gizmos.color = Color.black;
            UnityEngine.Gizmos.DrawWireCube(boundsCenter, size);
        }

        private void DrawTileBounds()
        {
            var roomBounds = _room.Bounds;
            var tiles      = _room.Info.Tiles;
            var min        =  roomBounds.Min;
            var max        =  roomBounds.Max;
            var grid       = Grid.Default;

            for (int dx = min.x; dx <= max.x; dx++)
            {
                for (int dz = min.z; dz <= max.z; dz++)
                {
                    var dGridIndex = new int3(dx, roomBounds.CenterIndex.y, dz);
                    var dWorld = grid.ToWorld(dGridIndex);

                    if (tiles.Contains(dGridIndex))
                    {
                        UnityEngine.Gizmos.color = Color.whiteSmoke;
                        UnityEngine.Gizmos.DrawCube(dWorld, new Vector3(1.0f, 0.45f, 1.0f));
                    }
                    else
                    {
                        var transparent = Color.whiteSmoke;
                        transparent.a = 0.5f;
                    
                        UnityEngine.Gizmos.color = transparent;
                        UnityEngine.Gizmos.DrawCube(dWorld, Vector3.one * 0.45f);
                    }
                }
            }
        }

        private void DrawAreaTiles()
        {
            var roomBounds = _room.Bounds;
            var min        =  roomBounds.Min;
            var max        =  roomBounds.Max;
            var grid       = Grid.Default;

            for (int dx = min.x - 3; dx <= max.x + 3; dx++)
            {
                for (int dz = min.z - 3; dz <= max.z + 3; dz++)
                {
                    if (dx >= min.x && dx <= max.x &&
                        dz >= min.z && dz <= max.z) continue;
                    
                    var dGridIndex = new int3(dx, 0, dz);
                    var dWorld     = grid.ToWorld(dGridIndex);

                    var transparent = Color.darkOrange;
                    transparent.a = 0.5f;
                    
                    UnityEngine.Gizmos.color = transparent;
                    UnityEngine.Gizmos.DrawCube(dWorld, new Vector3(1.0f, 0.25f, 1.0f));
                }
            }
        }
    }
}