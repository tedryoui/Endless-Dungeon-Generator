using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Attributes;
using JetBrains.Annotations;
using Service.Concrete;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "Room", menuName = "World Design/Room", order = 0)]
    public class RoomScriptableObject : ScriptableObject
    {
        [Serializable]
        public struct BoundsInfo
        {
            public int3 Min;
            public int3 Max;
        }
        
        [Serializable]
        public struct TileDescription
        {
            public string     Identity;
            public int3       Point;
            public quaternion Rotation;
        }
        
        [Serializable]
        public struct LocalSnapshot
        {
            [Serializable]
            public struct TileOperation
            {
                public enum OperationType { Add, Remove }

                public OperationType   Type;
                public TileDescription Tile;
            }
            
            public string              Identity;
            public List<TileOperation> Operations;
        }
        
        [Serializable]
        public struct GateInformation
        {
            public int3      Point;
            public Direction Direction;
            public string    SnapshotIdentity;
            public string    TunnelIdentity;
            public int3      Pivot;
        }
        
        [SerializeField] private string                   _identity = "CST_DEF_001";
        [SerializeField] private RoomType                 _type;
        
        [Obsolete]
        [SerializeField, HideInInspector] private AssetReferenceGameObject _reference;

        [Space(18)]
        [SerializeField, DisabledProperty(applyToCollection: true)] private List<TileDescription>   _tileMatrix;
        [SerializeField, DisabledProperty(applyToCollection: true)] private List<LocalSnapshot> _snapshots;
        
        [Space(18)]
        [SerializeField, DisabledProperty] private BoundsInfo _bounds;
        [SerializeField, DisabledProperty] private int3 _size;
        [Space(9)]
        [Tooltip("Size should always being equal to 1")]
        [SerializeField, DisabledProperty(applyToCollection: true)] private List<GateInformation> _enterGates;
        [SerializeField, DisabledProperty(applyToCollection: true)] private List<GateInformation> _exitGates;

        public string Identity => _identity;
        public RoomType Type => _type;
        
        [Obsolete]
        public AssetReferenceGameObject Reference => _reference;

        public Dictionary<int3, TileDescription> TileMatrix => _tileMatrix.ToDictionary(k => k.Point, v => v);
        public ReadOnlyCollection<LocalSnapshot> Snapshots  => _snapshots.AsReadOnly();
        
        public BoundsInfo Bounds => _bounds;
        public int3 Size => _size;
        
        public ReadOnlyCollection<GateInformation> EnterGates => _enterGates.AsReadOnly();
        public ReadOnlyCollection<GateInformation> ExitGates => _exitGates.AsReadOnly();

        public void Clear()
        {
            _tileMatrix = new List<TileDescription>();
            _snapshots = new List<LocalSnapshot>();
            
            _bounds = new BoundsInfo();
            _size = int3.zero;
            
            _enterGates = new List<GateInformation>();
            _exitGates = new List<GateInformation>();
        }
    }
}