using System;
using System.Collections.Extension;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attributes;
using Core.Scripts.Helpers;
using Events;
using Extensions;
using Mechanics.World_Designer;
using NUnit.Framework;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Grid = Mechanics.World_Designer.Grid;
using TileType = Mechanics.World_Designer.TileScriptableObject.TileType;

namespace Service.Concrete
{
    public partial class WorldDesignService : MonoService
    {
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private WorldDesignerProjectPreferences designerProjectPreferences;

        [Space(10)]
        [SerializeField, DisabledProperty] private TreeNode<RuntimeDescriptor> _root;
        private uint                    _nodeCount;

        public uint NextNodeIndex => _nodeCount + 1;

        private Dictionary<int3, uint>                            _worldMatrix;
        private Dictionary<int3, TileScriptableObject.TileType> _worldOutput;

        public event Action onWorldChanged;
        
        public override void Dispose()
        {
            Debug.Log("<color=white>World design service disposed</color>");
        }

        public override void Initialize()
        {
            Debug.Log("<color=green>World design service initialized</color>");

            onWorldChanged = delegate { };
            
            EventBus.Instance.Subscribe<ServicesRegisteredEvent>(InitializeWorld);
        }

        public RuntimeDescriptor GetRuntimeDescriptor(uint nodeIndex)
        {
            foreach (var node in _root)
            {
                if (node.Value.Index.Equals(nodeIndex))
                    return node.Value;
            }
            
            throw new KeyNotFoundException($"Node {nodeIndex} not found");
        }

        private TreeNode<RuntimeDescriptor> GetNode(uint nodeIndex)
        {
            foreach (var node in _root)
            {
                if (node.Value.Index.Equals(nodeIndex))
                    return node;
            }
            
            throw new KeyNotFoundException($"Node {nodeIndex} not found");
        }

        private void InitializeWorld(ServicesRegisteredEvent obj)
        {
            if (_root is null || !_root.Value.IsValid())
            {
                _worldMatrix = new Dictionary<int3, uint>();
                _worldOutput = new Dictionary<int3, TileScriptableObject.TileType>();
                
                CreateEntranceRoom();
            }
        }

        private void CreateEntranceRoom()
        {
            var roomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Type == RoomType.Entrance);
            Place(null, roomScriptableObject.Identity, 0);
        }
        
        public void Place(RuntimeDescriptor? parent, string identity, int exitIndex)
        {
            var structure = new RuntimeDescriptor(
                identity: identity,
                index: _root == null ? 0 : _nodeCount
            );

            if (parent is null)
            {
                // var alignInformation = new RuntimeDescriptor.AlignmentData
                // {
                //     Offset   = int3.zero,
                //     Rotation = new List<int>() { 90, -90, 180, 0 }.GetRandom()
                // };
                //
                // structure = structure.AssignAlignment(alignInformation);
                
                _root      = new TreeNode<RuntimeDescriptor>(structure);
                _nodeCount = 1;

                DisplayNode(_root);
            }
            else
            {
                var parentNode = GetNode(parent.Value.Index);

                if (!IsParentExitClear(parentNode, exitIndex))
                {
                    Debug.LogWarning("There is an obstacle!");
                    return;
                }

                var alignInformation = BuildAlignmentData(parentNode.Value, exitIndex, structure);
                structure = structure.AssignAlignment(alignInformation);
                var connectionInformation  = BuildConnectionData(parentNode.Value, structure, exitIndex, out var result);
                structure = structure.AssignConnection(connectionInformation);

                // if (result == false)
                //     throw new Exception("Room can not being placed here!");
                
                var node = parentNode.AddChild(structure);
                _nodeCount++;

                DisplayNode(node);
            }

            RegisterInWorldMatrix(structure);
            onWorldChanged?.Invoke();
        }

        private bool IsParentExitClear(TreeNode<RuntimeDescriptor> parentNode, int exitIndex)
        {
            var checkPattern = new[]
            {
                new int3(1, 0, 2), new int3(2,  0, 2), new int3(3,  0, 2),
                new int3(0, 0, 1), new int3(1,  0, 1), new int3(2,  0, 1), new int3(3,  0, 1), new int3(4,  0, 1),
                new int3(0, 0, 0), new int3(1,  0, 0), new int3(2,  0, 0), new int3(3,  0, 0), new int3(4,  0, 0),
                new int3(0, 0, -1), new int3(1, 0, -1), new int3(2, 0, -1), new int3(3, 0, -1), new int3(4, 0, -1),
                new int3(1, 0, -2), new int3(2, 0, -2), new int3(3, 0, -2)
            };
            
            var scriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(parentNode.Value.Identity));
            var exitDirection = scriptableObject.ExitGates[exitIndex].Direction;
            var rotationMatrix = float4x4.RotateY(math.radians(parentNode.Value.Alignment.Rotation));

            var directionRaw = exitDirection switch
            {
                Direction.Forward  => new int3(0,  0, 1),
                Direction.Backward => new int3(0,  0, -1),
                Direction.Left     => new int3(-1, 0, 0),
                Direction.Right    => new int3(1,  0, 0),
                _                  => throw new ArgumentOutOfRangeException()
            };
            
            directionRaw = (int3)math.round(math.transform(rotationMatrix, directionRaw));

            if (directionRaw.Equals(new int3(0, 0, 1)))
                exitDirection = Direction.Forward;
            else if (directionRaw.Equals(new int3(0, 0, -1)))
                exitDirection = Direction.Backward;
            else if (directionRaw.Equals(new int3(-1, 0, 0)))
                exitDirection = Direction.Left;
            else if (directionRaw.Equals(new int3(1, 0, 0)))
                exitDirection = Direction.Right;
            
            var rotation = exitDirection switch
            {
                Direction.Forward  => -90,
                Direction.Backward => 90,
                Direction.Left     => 180,
                Direction.Right    => 0,
                _                  => throw new ArgumentOutOfRangeException()
            };

            var checkPatternRotationMatrix = float4x4.RotateY(math.radians(rotation));
            var checkPatternBounds = exitDirection switch
            {
                Direction.Forward  => (Min: new int3(-2, 0, 0), Max: new int3(2,  0, 4)),
                Direction.Backward => (Min: new int3(-2, 0, -4), Max: new int3(2, 0, 0)),
                Direction.Left     => (Min: new int3(-4, 0, -2), Max: new int3(0, 0, 2)),
                Direction.Right    => (Min: new int3(0,  0, -2), Max: new int3(4, 0, 2)),
                _                  => throw new ArgumentOutOfRangeException()
            };

            var trs       = parentNode.Value.TRS;
            var dataPoint = scriptableObject.ExitGates[exitIndex].Point + scriptableObject.ExitGates[exitIndex].Pivot;
            var point     = (int3)math.round(math.transform(trs, dataPoint));
            var matrix    = GetWorldMatrixPartition(point + checkPatternBounds.Min, point + checkPatternBounds.Max, out _);

            foreach (var dataDelta in checkPattern)
            {
                var delta      = (int3)math.round(math.transform(checkPatternRotationMatrix, dataDelta));
                var finalPoint = point + delta;

                if (!matrix[finalPoint].Equals(0))
                    return false;
            }

            return true;
        }

        private void RegisterInWorldMatrix(RuntimeDescriptor structure)
        {
            var scriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(structure.Identity));

            foreach (var tileDescription in scriptableObject.TileMatrix)
            {
                var tilePoint = (int3)math.round(math.transform(structure.TRS, tileDescription.Value.Point));

                if (_worldMatrix.ContainsKey(tilePoint))
                    _worldMatrix[tilePoint] = structure.Index + 1;
                else
                    _worldMatrix.TryAdd(tilePoint, structure.Index + 1);

                var tileScriptableObject = designerProjectPreferences.Tiles.First(x => x.Identity.Equals(tileDescription.Value.Identity));
                
                if (_worldOutput.ContainsKey(tilePoint))
                    _worldOutput[tilePoint] = tileScriptableObject.Type;
                else
                    _worldOutput.TryAdd(tilePoint, tileScriptableObject.Type);
            }
        }

        public Dictionary<int3, uint> GetWorldMatrixPartition(int3 from, int3 to, out int2 size)
        {
            var dictionary = new Dictionary<int3, uint>();

            for (int z = from.z; z <= to.z; z++)
            {
                for (int x = from.x; x <= to.x; x++)
                {
                    int3 point = new int3(x, 0, z);
                    
                    if (_worldMatrix.TryGetValue(point, out uint value) && value > 0)
                        dictionary[point] = value;
                    else
                        dictionary[point] = 0;
                }
            }

            size = new int2(math.abs(to.x - from.x), math.abs(to.z - from.z)) + 1;
            return dictionary;
        }

        public void Add(uint parentIndex, string identity, int exitIndex)
        {
            var parentRuntimeDescriptor = GetRuntimeDescriptor(parentIndex);
            
            Place(parentRuntimeDescriptor, identity, exitIndex);
        }

        #region Visualizing methods

        private async void DisplayNode(TreeNode<RuntimeDescriptor> node)
        {
            var runtimeDescriptor = node.Value;
            var roomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(runtimeDescriptor.Identity));
            
            var gObj         = new GameObject($"ROOM: [{runtimeDescriptor.Index}]");
            var basicVisuals = new GameObject("Basic Visuals");
            
            gObj.transform.SetParent(_worldRoot);
            basicVisuals.transform.SetParent(gObj.transform);

            ReconstructRoomIntoTransform(runtimeDescriptor, basicVisuals.transform, out var taskCompletions);
            
            if (node.Parent is not null)
            {
                var parentRuntimeDescriptor = node.Parent.Value;
                var parentRoomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(parentRuntimeDescriptor.Identity));
                
                ReconstructRoomSnapshot(parentRuntimeDescriptor, parentRoomScriptableObject.ExitGates[(int)runtimeDescriptor.Connection.ExitIndex].SnapshotIdentity);
            
                await Task.WhenAll(taskCompletions);
                
                ReconstructRoomSnapshot(runtimeDescriptor, roomScriptableObject.EnterGates[0].SnapshotIdentity);
                ReconstructTunnel(node.Value);
            }
        }

        private void ReconstructTunnel(RuntimeDescriptor nodeValue)
        {
            var grid = Grid.Default;
            var tunnelObject = new GameObject($"TUNNEL: [{nodeValue.Index}]");
            tunnelObject.transform.SetParent(_worldRoot);
            
            var tunnelExtends = 1;
            var neighbours    = new List<int3>();

            for (int x = -tunnelExtends; x <= tunnelExtends; x++)
                for (int z = -tunnelExtends; z <= tunnelExtends; z++)
                    neighbours.Add(new int3(x, 0, z));

            var path         = nodeValue.Connection.Path.Skip(1).SkipLast(1);
            var pathExtended = path.SelectMany(x => neighbours.Select(y => y + x)).Distinct().ToList();
            var pathTypes    = new Dictionary<int3, TileType>();

            foreach (var point in pathExtended)
            {
                pathTypes[point] = TileType.Floor;

                if (!_worldOutput.TryAdd(point, pathTypes[point]))
                    _worldOutput[point] = pathTypes[point];
            }

            foreach (var point in pathExtended)
            {
                var type = _worldOutput[point];

                if (type.Equals(TileType.Floor))
                {
                    if (!_worldOutput.TryGetValue(point + new int3(1, 0, 0), out TileType rightType) || rightType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(-1, 0, 0), out TileType leftType) || leftType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(0, 0, 1), out TileType forwardType) || forwardType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(0, 0, -1), out TileType backwardType) || backwardType.Equals(TileType.Void))
                        type = TileType.Wall;
                    
                    if (!_worldOutput.TryGetValue(point + new int3(1, 0, 1), out TileType topRightType) || topRightType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(-1, 0, -1), out TileType bottomLeftType) || bottomLeftType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(-1, 0, 1), out TileType topLeftType) || topLeftType.Equals(TileType.Void))
                        type = TileType.Wall;
                    if (!_worldOutput.TryGetValue(point + new int3(1, 0, -1), out TileType bottomRightType) || bottomRightType.Equals(TileType.Void))
                        type = TileType.Wall;
                }
                
                pathTypes[point] = type;
                _worldOutput[point] = type;
            }

            foreach (var point in pathExtended)
            {
                var type    = pathTypes[point];
                if (type.Equals(TileType.Void)) continue;
                
                var bitmask = (byte)0;
                
                if (_worldOutput.TryGetValue(point + new int3(1, 0, 0), out TileType rightType) && rightType.Equals(type))
                    bitmask |= 1 << 0;
                if (_worldOutput.TryGetValue(point + new int3(-1, 0, 0), out TileType leftType) && leftType.Equals(type))
                    bitmask |= 1 << 1;
                if (_worldOutput.TryGetValue(point + new int3(0, 0, 1), out TileType forwardType) && forwardType.Equals(type))
                    bitmask |= 1 << 2;
                if (_worldOutput.TryGetValue(point + new int3(0, 0, -1), out TileType backwardType) && backwardType.Equals(type))
                    bitmask |= 1 << 3;

                var suitableTiles        = designerProjectPreferences.Tiles.Where(x => x.Type.Equals(type)).ToArray();
                var tileScriptableObject = (TileScriptableObject)null;
                var tileRotation         = 0;

                if (type.Equals(TileType.Floor))
                {
                    tileScriptableObject = suitableTiles.First();
                }
                else
                {
                    foreach (var suitableTile in suitableTiles)
                    {
                        if (tileScriptableObject != null)
                            break;
                        
                        foreach (var rotation in new int[] {0, 90, -90, 180})
                        {
                            var otherBitmask = (byte)0;

                            var matrix         = float4x4.RotateY(math.radians(rotation));
                            
                            var rightDirection = ToDirection((int3)math.round(math.transform(matrix, new int3(1, 0, 0))));
                            var leftDirection = ToDirection((int3)math.round(math.transform(matrix, new int3(-1, 0, 0))));
                            var forwardDirection = ToDirection((int3)math.round(math.transform(matrix, new int3(0, 0, 1))));
                            var backwardDirection = ToDirection((int3)math.round(math.transform(matrix, new int3(0, 0, -1))));
                    
                            if (suitableTile.Outputs.Contains(rightDirection))
                                otherBitmask |= 1 << 0;
                            if (suitableTile.Outputs.Contains(leftDirection))
                                otherBitmask |= 1 << 1;
                            if (suitableTile.Outputs.Contains(forwardDirection))
                                otherBitmask |= 1 << 2;
                            if (suitableTile.Outputs.Contains(backwardDirection))
                                otherBitmask |= 1 << 3;
                    
                            if (otherBitmask.Equals(bitmask))
                            {
                                tileScriptableObject = suitableTile;
                                tileRotation         = rotation;
                                break;
                            }
                            
                            Debug.Log(otherBitmask + "  " + bitmask);
                        }
                    }
                }

                if (tileScriptableObject == null)
                {
                    Debug.LogError("No suited tile founded!");
                    continue;
                }
                
                var tilePrefab    = tileScriptableObject.Prefab;
                var worldPoint    = grid.ToWorld(point);
                var worldRotation = quaternion.Euler(new float3(0.0f, math.radians(tileRotation), 0.0f));
                
                var operation = GameObject.InstantiateAsync(tilePrefab, 1, worldPoint, worldRotation, new InstantiateParameters
                {
                    parent            = tunnelObject.transform,
                    scene             = SceneManager.GetActiveScene(),
                    worldSpace        = true,
                    originalImmutable = true
                });

                operation.completed += _ =>
                {
                    var tileGameObject = operation.Result[0];

                    tileGameObject.name = $"TILE: [{point.x},{point.y},{point.z}]";
                };
            }
        }

        private int3 ToInt3(Direction direction)
        {
            return direction switch
            {
                Direction.Forward  => new int3(0,  0, 1),
                Direction.Backward => new int3(0,  0, -1),
                Direction.Left     => new int3(-1, 0, 0),
                Direction.Right    => new int3(1,  0, 0),
                _                  => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        private Direction ToDirection(int3 directionRaw)
        {
            if (directionRaw.Equals(new int3(-1, 0, 0)))
                return Direction.Left;
            if (directionRaw.Equals(new int3(1, 0, 0)))
                return Direction.Right;
            if (directionRaw.Equals(new int3(0, 0, -1)))
                return Direction.Backward;
            if (directionRaw.Equals(new int3(0, 0, 1)))
                return Direction.Forward;
            
            throw new Exception("Invalid direction!");
        }

        private void OnDrawGizmos()
        {
            var grid = Grid.Default;
            
            foreach (var node in _root)
            {
                if (!node.Value.IsValid()) continue;
                
                var tunnel = node.Value.Connection;

                if (tunnel.Path != null)
                {
                    var point = tunnel.Path[0];
                    Gizmos.color = Color.forestGreen;
                    Gizmos.DrawSphere(grid.ToWorld(point), 0.125f);

                    if (tunnel.Path.Length >= 2)
                    {
                        foreach (var element in tunnel.Path.Skip(1))
                        {
                            Gizmos.DrawSphere(grid.ToWorld(element), 0.125f);
                            Gizmos.DrawLine(grid.ToWorld(point), grid.ToWorld(element));

                            point = element;
                        }
                    }
                }
            }
        }

        private void ReconstructRoomIntoTransform(RuntimeDescriptor runtimeDescriptor, Transform parent, out List<Task> taskCompletions)
        {
            taskCompletions = new List<Task>();
            
            var roomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(runtimeDescriptor.Identity));
            var tileMatrix     = roomScriptableObject.TileMatrix;
            
            parent.transform.Clear();
            
            var grid = Grid.Default;

            foreach (var tile in tileMatrix)
            {
                var alignedPoint = (int3)math.round(math.transform(runtimeDescriptor.TRS, tile.Value.Point));
                var gridIndex = alignedPoint;
                var tileIdentity         = tile.Value.Identity;
                var tileScriptableObject = designerProjectPreferences.Tiles.FirstOrDefault(x => x.Identity.Equals(tileIdentity));
                
                if (tileScriptableObject == null) continue;
                
                var worldPoint = grid.ToWorld(gridIndex);
                var rotation   = quaternion.Euler(
                    x: math.Euler(tile.Value.Rotation).x,
                    y: math.Euler(tile.Value.Rotation).y + math.radians(runtimeDescriptor.Alignment.Rotation),
                    z: math.Euler(tile.Value.Rotation).z);
                var tilePrefab = tileScriptableObject.Prefab;

                var taskCompletionSource = new TaskCompletionSource<bool>(false);

                var operation = GameObject.InstantiateAsync(tilePrefab, 1, (Vector3)worldPoint, rotation.ToUnity(), new InstantiateParameters
                {
                    parent            = parent,
                    scene             = SceneManager.GetActiveScene(),
                    worldSpace        = true,
                    originalImmutable = true
                });

                operation.completed += _ =>
                {
                    var tileGameObject = operation.Result[0];

                    tileGameObject.name = $"TILE: [{tile.Value.Point.x},{tile.Value.Point.y},{tile.Value.Point.z}]";

                    taskCompletionSource.TrySetResult(true);
                };
                
                taskCompletions.Add(taskCompletionSource.Task);
            }
        }

        private void ReconstructRoomSnapshot(RuntimeDescriptor runtimeDescriptor, string snapshotIdentity)
        {
            var roomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(runtimeDescriptor.Identity));
            
            var roomGameObject       = _worldRoot.Find($"ROOM: [{runtimeDescriptor.Index}]");
            var basicVisuals         = roomGameObject.Find("Basic Visuals");
            var basicVisualsChildren = basicVisuals.GetChildren(1);
            
            var snapshot = roomScriptableObject.Snapshots.FirstOrDefault(x => x.Identity.Equals(snapshotIdentity));
            var grid     = Grid.Default;
            
            foreach (var operation in snapshot.Operations)
            {
                if (operation.Type is RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Add)
                {
                    var alignedPoint         = (int3)math.round(math.transform(runtimeDescriptor.TRS, operation.Tile.Point));
                    var gridIndex            = alignedPoint;
                    var tileIdentity         = operation.Tile.Identity;
                    var tileScriptableObject = designerProjectPreferences.Tiles.FirstOrDefault(x => x.Identity.Equals(tileIdentity));
                
                    if (tileScriptableObject == null) continue;
                    
                    var worldPoint = grid.ToWorld(gridIndex);
                    var rotation   = quaternion.Euler(
                        x: math.Euler(operation.Tile.Rotation).x,
                        y: math.Euler(operation.Tile.Rotation).y + math.radians(runtimeDescriptor.Alignment.Rotation),
                        z: math.Euler(operation.Tile.Rotation).z);
                    var tilePrefab = tileScriptableObject.Prefab;
                
                    var asyncOperation = GameObject.InstantiateAsync(tilePrefab, 1, (Vector3)worldPoint, rotation.ToUnity(), new InstantiateParameters
                    {
                        parent            = basicVisuals.transform,
                        scene             = SceneManager.GetActiveScene(),
                        worldSpace        = true,
                        originalImmutable = true
                    });
                    
                    asyncOperation.completed += _ =>
                    {
                        var tileGameObject = asyncOperation.Result[0];

                        tileGameObject.name = $"TILE: [{operation.Tile.Point.x},{operation.Tile.Point.y},{operation.Tile.Point.z}]";
                    };
                } else if (operation.Type is RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Remove)
                {
                    foreach (var tile in basicVisualsChildren)
                    {
                        if (tile == null) continue;
                        if (tile.CompareTag(UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Tile)))
                        {
                            var position  = tile.transform.position;
                            var gridIndex = grid.ToGrid(position);
                            var identity  = FindTileIdentity(tile.gameObject);
                            
                            var expectedGridIndex = (int3)math.round(math.transform(runtimeDescriptor.TRS, operation.Tile.Point));
                    
                            if (gridIndex.Equals(expectedGridIndex) && identity.Equals(operation.Tile.Identity))
                                GameObject.Destroy(tile.gameObject);
                        }
                    }
                }
            }
        }
        
        private string FindTileIdentity(GameObject gameObject)
        {
            var label          = gameObject.GetComponent<Label>();
            var editorInstance = WorldDesignerProjectPreferences.EditorInstance;

            var result = editorInstance.Tiles.FirstOrDefault(x => label.HasLabel(x.Identity));
            
            if (result == null) throw new Exception($"Could not find tile of {gameObject}");
            
            return result.Identity;
        }

        #endregion
        

        private RuntimeDescriptor.AlignmentData BuildAlignmentData(
            RuntimeDescriptor parent, 
            int exitIndex, 
            RuntimeDescriptor child)
        {
            var childRoomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(child.Identity));
            var parentRoomScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(parent.Identity));

            var parentRotationMatrix = float4x4.RotateY(math.radians(parent.Alignment.Rotation));
            var exitDirection        = parentRoomScriptableObject.ExitGates[exitIndex].Direction;
            var enterDirection       = childRoomScriptableObject.EnterGates[0].Direction;

            var directionRaw = exitDirection switch
            {
                Direction.Forward  => new int3(0, 0, 1),
                Direction.Backward => new int3(0, 0, -1),
                Direction.Left     => new int3(-1, 0, 0),
                Direction.Right    => new int3(1, 0, 0),
                _                  => throw new ArgumentOutOfRangeException()
            };
            
            directionRaw = (int3)math.round(math.transform(parentRotationMatrix, directionRaw));

            if (directionRaw.Equals(new int3(0, 0, 1)))
                exitDirection = Direction.Forward;
            else if (directionRaw.Equals(new int3(0, 0, -1)))
                exitDirection = Direction.Backward;
            else if (directionRaw.Equals(new int3(-1, 0, 0)))
                exitDirection = Direction.Left;
            else if (directionRaw.Equals(new int3(1, 0, 0)))
                exitDirection = Direction.Right;
            
            var rotationMatrixIndex = (x: (int)exitDirection, y: (int)enterDirection);
            var rotationMatrix = new float[][]
            {
                new[] { 180.0f, 0.0f, 90.0f, -90.0f },
                new[] { 0.0f, 180.0f, -90.0f, 90.0f },
                new[] { -90.0f, 90.0f, 180.0f, 0.0f },
                new[] { 90.0f, -90.0f, 0.0f, 180.0f },
            }.Transpose();

            var rotation = rotationMatrix[rotationMatrixIndex.x][rotationMatrixIndex.y];

            var parentTRS = parent.TRS;
            var childTRS = float4x4.TRS(
                translation: float3.zero,
                rotation: quaternion.Euler(
                    x: 0.0f,
                    y: math.radians(rotation),
                    z: 0.0f),
                scale: new float3(1, 1, 1));

            var exitPivot = parentRoomScriptableObject.ExitGates[exitIndex].Pivot;
            var enterPivot = childRoomScriptableObject.EnterGates[0].Pivot;
            var exitPoint  = (int3)math.round(math.transform(parentTRS, parentRoomScriptableObject.ExitGates[exitIndex].Point + exitPivot));
            var enterPoint = (int3)math.round(math.transform(childTRS, childRoomScriptableObject.EnterGates[0].Point + enterPivot));
            var bounds = child.GetBounds(childRoomScriptableObject, childTRS, out _);

            var gateOffset = AlignmentUtility.CalculateOffset(bounds, enterPoint, exitPoint);
            var offset     = PushOffOther(child, (int)rotation, gateOffset, parent, (uint)exitIndex);
            
            return new RuntimeDescriptor.AlignmentData()
            {
                Offset = offset, Rotation = (int)rotation
            };
        }

        private int3 PushOffOther(RuntimeDescriptor runtimeDescriptor, int yRotation, int3 initialOffset, RuntimeDescriptor parentRuntimeDescriptor, uint exitIndex)
        {
            var offset = int3.zero;
            var neighbors = new List<int3>();

            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    if (x == 0 && z == 0) continue;
                    
                    neighbors.Add(new int3(x, 0, z));
                }
            }
            
            var trs = float4x4.TRS(initialOffset, quaternion.Euler(0.0f, math.radians(yRotation), 0.0f), new float3(1, 1, 1));
            var scriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(runtimeDescriptor.Identity));
            var parentScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(parentRuntimeDescriptor.Identity));
            var bounds = runtimeDescriptor.GetBounds(scriptableObject, trs, out _);
            var tileMatrix = scriptableObject.TileMatrix;
            
            int minDistance = 6;

            var enterPointLocal = scriptableObject.EnterGates[0].Point + scriptableObject.EnterGates[0].Pivot;
            var exitPointLocal = parentScriptableObject.ExitGates[(int)exitIndex].Point +  parentScriptableObject.ExitGates[(int)exitIndex].Pivot;
            var exitPointWorld = (int3)math.round(math.transform(parentRuntimeDescriptor.TRS, exitPointLocal));
            var exitDirection = (int3)math.round(math.transform(float4x4.RotateY(math.radians(parentRuntimeDescriptor.Alignment.Rotation)), ToInt3(parentScriptableObject.ExitGates[(int)exitIndex].Direction)));
            
            int iteration = 0;
            
            while (true)
            {
                if (iteration >= 500) 
                    throw new Exception("Too many attempts to offset room!");
                
                var matrix   = GetWorldMatrixPartition(bounds.Min - 4  + offset, bounds.Max + 4 + offset, out _);
                var push     = new float3();
                
                var enterPointWorld = (int3)math.round(math.transform(trs, enterPointLocal)) + offset;
                var distance        = 0;
                
                if (ToDirection(exitDirection).Equals(Direction.Right))
                    distance = enterPointWorld.x - exitPointWorld.x;
                else if (ToDirection(exitDirection).Equals(Direction.Left))
                    distance = exitPointWorld.x - enterPointWorld.x;
                else if (ToDirection(exitDirection).Equals(Direction.Forward))
                    distance = enterPointWorld.z - exitPointWorld.z;
                else if (ToDirection(exitDirection).Equals(Direction.Backward))
                    distance = exitPointWorld.z - enterPointWorld.z;
                
                Debug.Log("Distance: " +  distance);
                if (math.abs(distance) <= minDistance) push += exitDirection * 100;
                
                foreach (var tileDescription in tileMatrix)
                {
                    var overlaps = new HashSet<uint>();
                    var point    = (int3)math.round(math.transform(trs, tileDescription.Value.Point)) + offset;

                    foreach (var neighbor in neighbors)
                    {
                        var otherPoint = point + neighbor;
                        
                        if (matrix.TryGetValue(otherPoint, out var value) && value != 0)
                            overlaps.Add(value - 1);
                    }

                    foreach (var overlap in overlaps)
                    {
                        var otherRuntimeDescription = GetRuntimeDescriptor(overlap);
                        var otherScriptableObject =
                            designerProjectPreferences.Rooms.First(x =>
                                x.Identity.Equals(otherRuntimeDescription.Identity));
                        var otherBounds = otherRuntimeDescription.GetBounds(otherScriptableObject, null, out _);
                        
                        var center = otherBounds.Min + new int3(
                            x: (int)math.round(otherBounds.Max.x - otherBounds.Min.x),
                            y: (int)math.round(otherBounds.Max.y - otherBounds.Min.y),
                            z: (int)math.round(otherBounds.Max.z - otherBounds.Min.z)) / 2;

                        var direction = point - center;
                        push += direction;
                    }
                }
                
                iteration++;
                if (push.Equals(new int3(0, 0, 0))) return offset + initialOffset;
                else offset += new int3(
                    math.abs(push.x) > math.abs(push.z) ? push.x > 0 ? 1 : -1 : 0, 
                    0, 
                    math.abs(push.z) >= math.abs(push.x) ? push.z > 0 ? 1 : -1 : 0);
            }
        }

        private RuntimeDescriptor.ConnectionData BuildConnectionData(
            RuntimeDescriptor parentRuntimeDescriptor, 
            RuntimeDescriptor childRuntimeDescriptor, 
            int               exitIndex,
            out bool result)
        {
            var connectionData = new RuntimeDescriptor.ConnectionData()
            {
                ExitIndex = (uint)exitIndex,
            };
            
            var parentScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(parentRuntimeDescriptor.Identity));
            var parentTRS = parentRuntimeDescriptor.TRS;
            var exit = parentScriptableObject.ExitGates[exitIndex];

            connectionData.TunnelIdentity = exit.TunnelIdentity;
            
            var childScriptableObject = designerProjectPreferences.Rooms.First(x => x.Identity.Equals(childRuntimeDescriptor.Identity));
            var childTRS = childRuntimeDescriptor.TRS;         
            var enter = childScriptableObject.EnterGates[0];

            var path           = new List<int3>();
            var pathExit       = new List<int3>();
            var pathEnter      = new List<int3>();
            var pathPathfinder = new List<int3>();

            var exitPoint  = (int3)math.round(math.transform(parentTRS, exit.Point + exit.Pivot));
            var enterPoint = (int3)math.round(math.transform(childTRS,  enter.Point + enter.Pivot));
            
            connectionData.From = exitPoint;
            connectionData.To   = enterPoint;

            var pFrom = (int3)default;
            var pTo = (int3)default;

            {
                var exitRotationMatrix = float4x4.RotateY(math.radians(parentRuntimeDescriptor.Alignment.Rotation));
                var exitDirection      = exit.Direction;
                var directionRaw = exitDirection switch
                {
                    Direction.Forward  => new int3(0,  0, 1),
                    Direction.Backward => new int3(0,  0, -1),
                    Direction.Left     => new int3(-1, 0, 0),
                    Direction.Right    => new int3(1,  0, 0),
                    _                  => throw new ArgumentOutOfRangeException()
                };

                directionRaw = (int3)math.round(math.transform(exitRotationMatrix, directionRaw));

                for (int offset = 0; offset <= 1; offset++)
                {
                    var offsetPoint = exitPoint + offset * directionRaw;

                    pathExit.Add(offsetPoint);

                    if (offsetPoint.Equals(enterPoint))
                    {
                        connectionData.Path = pathExit.ToArray();
                        result              = true;
                        return connectionData;
                    }
                }

                pFrom = exitPoint + directionRaw * 2;
            }
            
            {
                var enterRotationMatrix = float4x4.RotateY(math.radians(childRuntimeDescriptor.Alignment.Rotation));
                var enterDirection      = enter.Direction;
                var directionRaw = enterDirection switch
                {
                    Direction.Forward  => new int3(0,  0, 1),
                    Direction.Backward => new int3(0,  0, -1),
                    Direction.Left     => new int3(-1, 0, 0),
                    Direction.Right    => new int3(1,  0, 0),
                    _                  => throw new ArgumentOutOfRangeException()
                };

                directionRaw = (int3)math.round(math.transform(enterRotationMatrix, directionRaw));

                for (int offset = 0; offset <= 1; offset++)
                {
                    var offsetPoint = enterPoint + offset * directionRaw;
                    
                    if (pathExit.Contains(offsetPoint))
                    {
                        pathEnter.Reverse();
                        
                        connectionData.Path = pathExit.Concat(pathEnter).ToArray();
                        result              = true;
                        return connectionData;
                    }
                    
                    pathEnter.Add(offsetPoint);
                }
                
                pTo = enterPoint + directionRaw * 2;
            }

            var roomMatrix = childScriptableObject.TileMatrix.Select(x =>
            {
                var point = (int3)math.round(math.transform(childRuntimeDescriptor.TRS, x.Key));

                return point;
            }).ToList();

            var pathfinder = new AStarPathfinding(pFrom.xz, pTo.xz, 50, 10, (a, b, c) => ScanPathNode(a, b, c, roomMatrix));
            var pathfinderResult = pathfinder.FindPath();

            if (pathfinderResult == null || pathfinderResult.Count == 0)
            {
                result              = false;
                connectionData.Path = pathExit.Concat(pathEnter).ToArray();
                return connectionData;
            }

            pathEnter.Reverse();
            pathPathfinder = pathfinderResult.Select(x => new int3(x.x, 0, x.y)).ToList();
            
            connectionData.Path = pathExit.Concat(pathPathfinder).Concat(pathEnter).ToArray();

            result = true;
            return connectionData;
        }

        private void ScanPathNode(int2 center, int radius, Dictionary<int2, bool> cache, List<int3> roomMatrix)
        {
            var center3D  = new int3(center.x, 0, center.y);
            var matrix    = GetWorldMatrixPartition(center3D - radius - 6, center3D + radius + 6, out _);
            var neighbours = new List<int2>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    neighbours.Add(new int2(x, y));
                }
            }
            
            for (int x = -radius; x <= radius; x++)
            {
                 for (int y = -radius; y <= radius; y++)
                 {
                     int2 point = center + new int2(x, y);
             
                     if (cache.ContainsKey(point)) continue;
                     
                     cache[point] = true;

                     foreach (var neighbour in neighbours)
                     {
                         int2 neighbourPoint = point + neighbour;
                         int3 neighbourPoint3D = new int3(neighbourPoint.x, 0, neighbourPoint.y);

                         if (matrix.TryGetValue(neighbourPoint3D, out uint value) && !value.Equals(0))
                         {
                             cache[point] = false;
                             break;
                         } 
                         
                         // if (roomMatrix.Contains(neighbourPoint3D))
                         // {
                         //     cache[point] = false;
                         //     break;
                         // }
                     }
                 }
            }
        }

        //         private void CreateRoom(
//             RoomStructure structure, 
//             TreeNode<RoomStructure> node,
//             Action<TreeNode<RoomStructure>> onCreate = null)
//         {
//             var description = designerProjectPreferences.Rooms.First(x => x.Type == structure.Type && x.Identity == structure.Identity);
//             var operation      = description.Reference.LoadAssetAsync<GameObject>();
//
//             operation.Completed += (result) =>
//             {
//                 if (result.Result != null)
//                 {
//                     var asyncOperation = InstantiateAsync(result.Result, _worldRoot);
//
//                     asyncOperation.completed += (op) =>
//                     {
//                         var roomGameObject = asyncOperation.Result[0];
//                         var roomConnector  = roomGameObject.AddComponent<RoomConnector>();
//
//                         roomConnector.Node = node;
//                         node.Value = node.Value.AssignRoom(roomGameObject.GetComponent<Room>());
//                         node.Value.Room.UpdateData();
//                         
//                         description.Reference.ReleaseAsset();
//                         
//                         onCreate?.Invoke(node);
//                     };
//                 }
//             };
//         }
//
//         private async void AlignRoom(RoomStructure childStructure, RoomStructure parentStructure)
//         {
//             var worldGrid      = Grid.Default;
//             var exitIndex      = childStructure.ParentExitIndex;
//             var exitDirection  = parentStructure.Room.Info.ExitDirections[exitIndex];
//             var enterDirection = childStructure.Room.Info.EnterDirection;
//             
//             var rotationMatrixIndex = (x: (int)exitDirection, y: (int)enterDirection);
//             var rotationMatrix = new float[][]
//             {
//                 new[] { 180.0f,   0.0f,     90.0f,    -90.0f },
//                 new[] { 0.0f,     180.0f,   -90.0f,   90.0f },
//                 new[] { -90.0f,   90.0f,    180.0f,   0.0f },
//                 new[] { 90.0f,    -90.0f,   0.0f,     180.0f },
//             }.Transpose();
//             
//             childStructure.Room.transform.rotation = Quaternion.Euler(0.0f, rotationMatrix[rotationMatrixIndex.x][rotationMatrixIndex.y], 0.0f);
//
//             childStructure.Room.UpdateData();
//             
//             var exitGridIndex          = parentStructure.Room.Info.Exits[exitIndex];
//             var exitGridWorldPosition  = worldGrid.ToWorld(exitGridIndex);
//             var enterGridIndexPosition = childStructure.Room.Info.Enter;
//             var enterGridWorldPosition = worldGrid.ToWorld(enterGridIndexPosition);
//             var pivot                  = new Pivot(childStructure.Room.transform);
//             
//             var translateVector = new float3(
//                 x: exitDirection switch
//                 {
//                     Room.RoomInfo.Direction.Left  => -1,
//                     Room.RoomInfo.Direction.Right => 1,
//                     _                             => 0
//                 },
//                 y: 0f,
//                 z: exitDirection switch
//                 {
//                     Room.RoomInfo.Direction.Forward  => 1,
//                     Room.RoomInfo.Direction.Backward => -1,
//                     _                                => 0
//                 }
//             );
//             
//             pivot.SetPivotTo(enterGridWorldPosition);
//             pivot.SetPosition(exitGridWorldPosition);
//             childStructure.Room.transform.Translate(translateVector, Space.World);
//             
//             Debug.Log(@$"Setting Enter [{enterGridWorldPosition}] to exit [{exitGridWorldPosition}]
// Additional data:
// Exit World Position: [{exitGridWorldPosition}]
// Provided Rotation: [{rotationMatrix[rotationMatrixIndex.x][rotationMatrixIndex.y]}], ID: [{rotationMatrixIndex}], ID_RAW: [{exitDirection}, {enterDirection}]
// Translate: [{translateVector.x},  {translateVector.y}, {translateVector.z}]");
//
//             if (!childStructure.Room.IsValidPlacement(out _))
//                 PushRoom(childStructure);
//         }
//
//         private void PushRoom(RoomStructure childStructure)
//         {
//             var iterator = 0;
//             
//             while (!childStructure.Room.IsValidPlacement(out var obstacles, childStructure.Room.transform.GetSceneCenter()))
//             {
//                 iterator++;
//                 if (iterator > 100)
//                     throw new Exception("Too many attempts!");
//                 
//                 var pushDirection = new float3(0);
//
//                 foreach (var other in obstacles)
//                 {
//                     var otherRoom = other.gameObject.GetComponent<Room>();
//                     
//                     var roomCenter       = childStructure.Room.transform.position;
//                     var otherCenter      = otherRoom.transform.position;
//                     var toOtherDirection = roomCenter - otherCenter;
//                     
//                     pushDirection += (float3)toOtherDirection;
//                 }
//                 
//                 pushDirection = math.normalize(pushDirection);
//                 pushDirection = new float3(
//                     math.round(pushDirection.x),
//                     math.round(pushDirection.y),
//                     math.round(pushDirection.z));
//
//                 childStructure.Room.transform.position += (Vector3)pushDirection;
//             }
//         }
//
//         private void ConnectRoom(TreeNode<RoomStructure> node, RoomStructure nodeValue, RoomStructure parentNodeValue)
//         {
//             nodeValue.Room.UpdateData();
//             parentNodeValue.Room.UpdateData();
//             
//             var exitDirection = parentNodeValue.Room.Info.ExitDirections[nodeValue.ParentExitIndex];
//             var translateVector = new int2(
//                 x: exitDirection switch
//                 {
//                     Room.RoomInfo.Direction.Left  => -2,
//                     Room.RoomInfo.Direction.Right => 2,
//                     _                             => 0
//                 },
//                 y: exitDirection switch
//                 {
//                     Room.RoomInfo.Direction.Forward  => 2,
//                     Room.RoomInfo.Direction.Backward => -2,
//                     _                                => 0
//                 }
//             );
//
//             var enterDirection = nodeValue.Room.Info.EnterDirection;
//             var otherTranslateVector = new int2(
//                 x: enterDirection switch
//                 {
//                     Room.RoomInfo.Direction.Left  => -2,
//                     Room.RoomInfo.Direction.Right => 2,
//                     _                             => 0
//                 },
//                 y: enterDirection switch
//                 {
//                     Room.RoomInfo.Direction.Forward  => 2,
//                     Room.RoomInfo.Direction.Backward => -2,
//                     _                                => 0
//                 }
//             );
//             
//             var  exitIndex = nodeValue.ParentExitIndex;
//             int2 start     = parentNodeValue.Room.Info.Exits[exitIndex].xz + translateVector;
//             int2 end       = nodeValue.Room.Info.Enter.xz + otherTranslateVector;
//
//             AStarPathfinding pathfinding = new AStarPathfinding(start, end, 50, 10, (x,y,z) => GetNeighbors(x,y,z, nodeValue, parentNodeValue));
//             var              path    = pathfinding.FindPath().Select(x => new int3(x.x, 0, x.y)).ToList();
//             
//             var tunnelGameObject = new GameObject($"Tunnel [{nodeValue.Identity}, {parentNodeValue.Identity}]");
//             var tunnel           = tunnelGameObject.AddComponent<Tunnel>();
//             
//             tunnelGameObject.transform.parent = _worldRoot;
//             
//             path.Insert(0, parentNodeValue.Room.Info.Exits[exitIndex]);
//             path.Insert(0, parentNodeValue.Room.Info.Exits[exitIndex] + new int3(translateVector.x, 0, translateVector.y) / 2);
//             path.Add(nodeValue.Room.Info.Enter);
//             path.Add(nodeValue.Room.Info.Enter + new int3(otherTranslateVector.x, 0, otherTranslateVector.y) / 2);
//             
//             var roomTunnel =
//                 new RoomStructure.RoomTunnel(parentNodeValue.Room.Info.Exits[exitIndex], nodeValue.Room.Info.Enter, path.ToArray(), tunnel);
//             
//             node.Value = nodeValue.AssignTunnel(roomTunnel);
//             
//             tunnel.Build(roomTunnel);
//         }
//
//         private string RoomTag => UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Room);
//
//         private void GetNeighbors(int2 center, int radius, Dictionary<int2, bool> cache, RoomStructure child, RoomStructure parent)
//         {
//             for (int x = -radius; x <= radius; x++)
//             {
//                 for (int y = -radius; y <= radius; y++)
//                 {
//                     int2 point = center + new int2(x, y);
//             
//                     if (cache.ContainsKey(point)) continue;
//
//                     var colliders = Physics.OverlapBox(new float3(point.x, 0, point.y), Vector3.one / 2.0f, Quaternion.identity,
//                         LayerMask.GetMask("Default"),
//                         QueryTriggerInteraction.Collide);
//
//                     bool isWalkable = colliders.Count(other => other.gameObject.CompareTag(RoomTag) &&
//                                                                other.gameObject.GetComponent<Room>().BoundsCollider == other) == 0;
//             
//                     cache[point] = isWalkable;
//                 }
//             }
//         }
    }
}