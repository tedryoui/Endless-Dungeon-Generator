using System;
using System.Collections.Extension;
using System.Linq;
using System.Threading.Tasks;
using Attributes;
using Core.Scripts.Helpers;
using Events;
using Extensions;
using Mechanics.World_Designer;
using Unity.Mathematics;
using UnityEngine;
using Grid = Mechanics.World_Designer.Grid;

namespace Service.Concrete
{
    public partial class WorldDesignService : MonoService
    {
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private WorldDesignSet _designSet;

        [Space(10)]
        [SerializeField, DisabledProperty] private TreeNode<RoomStructure> _root;
        private uint                    _nodeCount;

        public uint NextNodeIndex => _nodeCount + 1;
        
        public override void Dispose()
        {
            Debug.Log("<color=white>World design service disposed</color>");
        }

        public override void Initialize()
        {
            Debug.Log("<color=green>World design service initialized</color>");
            
            EventBus.Instance.Subscribe<ServicesRegistredEvent>(InitializeWorld);
        }

        private void InitializeWorld(ServicesRegistredEvent obj)
        {
            if (_root is null || !_root.Value.IsValid)
                CreateEntranceRoom();
        }

        private void CreateEntranceRoom()
        {
            var random    = _designSet.TakeRandom(RoomType.Entrance);
            var structure = new RoomStructure(0, 0, 0, random);
            
            Place(structure, null);
        }

        public void Place(RoomStructure structure, TreeNode<RoomStructure> parentNode)
        {
            if (parentNode is null)
            {
                _root      = new TreeNode<RoomStructure>(structure);
                _nodeCount = 0;
                CreateRoom(structure, _root);
                
                return;
            }

            var node = parentNode.AddChild(structure);
            CreateRoom(structure, node, (v) =>
            {
                AlignRoom(node.Value, parentNode.Value);
                ConnectRoom(node.Value, parentNode.Value);
            });
        }

        private void CreateRoom(
            RoomStructure structure, 
            TreeNode<RoomStructure> node,
            Action<TreeNode<RoomStructure>> onCreate = null)
        {
            var description = _designSet.Take(structure.Type, structure.Identity);
            var operation      = description.Reference.LoadAssetAsync<GameObject>();

            operation.Completed += (result) =>
            {
                if (result.Result != null)
                {
                    var asyncOperation = InstantiateAsync(result.Result, _worldRoot);

                    asyncOperation.completed += (op) =>
                    {
                        var roomGameObject = asyncOperation.Result[0];
                        var roomConnector  = roomGameObject.AddComponent<RoomConnector>();

                        roomConnector.Node = node;
                        node.Value = node.Value.AssignRoom(roomGameObject.GetComponent<Room>());
                        node.Value.Room.UpdateData();
                        
                        description.Reference.ReleaseAsset();
                        
                        onCreate?.Invoke(node);
                    };
                }
            };
        }

        private async void AlignRoom(RoomStructure childStructure, RoomStructure parentStructure)
        {
            var worldGrid      = Grid.Create(new float3(1.0f), Grid.GridMode.World);
            var exitIndex      = childStructure.ParentExitIndex;
            var exitDirection  = parentStructure.Room.Info.ExitDirections[exitIndex];
            var enterDirection = childStructure.Room.Info.EnterDirection;
            
            var rotationMatrixIndex = (x: (int)exitDirection, y: (int)enterDirection);
            var rotationMatrix = new float[][]
            {
                new[] { 180.0f,   0.0f,     90.0f,    -90.0f },
                new[] { 0.0f,     180.0f,   -90.0f,   90.0f },
                new[] { -90.0f,   90.0f,    180.0f,   0.0f },
                new[] { 90.0f,    -90.0f,   0.0f,     180.0f },
            }.Transpose();
            
            childStructure.Room.transform.rotation = Quaternion.Euler(0.0f, rotationMatrix[rotationMatrixIndex.x][rotationMatrixIndex.y], 0.0f);

            childStructure.Room.UpdateData();
            
            var exitGridIndex          = parentStructure.Room.Info.Exits[exitIndex] + (int3)parentStructure.Room.Bounds.Center;
            var exitGridWorldPosition  = worldGrid.ToWorld(exitGridIndex);
            var enterGridIndexPosition = childStructure.Room.Info.Enter;
            var enterGridWorldPosition = worldGrid.ToWorld(enterGridIndexPosition);
            var pivot                  = new Pivot(childStructure.Room.transform);
            
            var translateVector = new float3(
                x: exitDirection switch
                {
                    Room.RoomInfo.Direction.Left  => -1,
                    Room.RoomInfo.Direction.Right => 1,
                    _                             => 0
                },
                y: 0f,
                z: exitDirection switch
                {
                    Room.RoomInfo.Direction.Forward  => 1,
                    Room.RoomInfo.Direction.Backward => -1,
                    _                                => 0
                }
            );
            
            pivot.SetPivotTo(enterGridWorldPosition);
            pivot.SetPosition(exitGridWorldPosition);
            childStructure.Room.transform.Translate(translateVector, Space.World);
            
            Debug.Log(@$"Setting Enter [{enterGridWorldPosition}] to exit [{exitGridWorldPosition}]
Additional data:
Exit World Position: [{exitGridWorldPosition}]
Provided Rotation: [{rotationMatrix[rotationMatrixIndex.x][rotationMatrixIndex.y]}], ID: [{rotationMatrixIndex}], ID_RAW: [{exitDirection}, {enterDirection}]
Translate: [{translateVector.x},  {translateVector.y}, {translateVector.z}]");

            if (!childStructure.Room.IsValidPlacement(out _))
                PushRoom(childStructure);
        }

        private async void PushRoom(RoomStructure childStructure)
        {
            var iterator = 0;
            
            while (!childStructure.Room.IsValidPlacement(out var obstacles, childStructure.Room.transform.GetSceneCenter()))
            {
                iterator++;
                if (iterator > 100)
                    throw new Exception("Too many attempts!");
                
                var pushDirection = new float3(0);

                foreach (var other in obstacles)
                {
                    var roomCenter = childStructure.Room.transform.position +
                                     childStructure.Room.transform.GetSceneCenter();
                    var otherCenter      = other.transform.position + other.transform.GetSceneCenter();
                    var toOtherDirection = roomCenter - otherCenter;
                    
                    pushDirection += (float3)toOtherDirection;
                }
                
                pushDirection = math.normalize(pushDirection);
                pushDirection = new float3(
                    math.round(pushDirection.x),
                    math.round(pushDirection.y),
                    math.round(pushDirection.z));

                childStructure.Room.transform.position += (Vector3)pushDirection;
            }
        }

        private void ConnectRoom(RoomStructure nodeValue, RoomStructure parentNodeValue)
        {
            
        }
    }
}