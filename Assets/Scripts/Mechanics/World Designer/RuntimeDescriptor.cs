using System;
using Attributes;
using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer
{
    [Serializable]
    public struct RuntimeDescriptor
    {
        [Serializable]
        public struct ConnectionData
        {
            public string TunnelIdentity;
            public uint   ExitIndex;
            public int3   From;
            public int3   To;
            public int3[] Path;
        }
        
        [Serializable]
        public struct AlignmentData
        {
            public int3 Offset;
            public int  Rotation;
        }
        
        [SerializeField, DisabledProperty] private string     _identity;
        [SerializeField, DisabledProperty] private uint       _index;
        [SerializeField, DisabledProperty] private AlignmentData  _alignment;
        [SerializeField, DisabledProperty] private ConnectionData _connection;

        public event Action<RuntimeDescriptor> onChanged;
        
        public string Identity => _identity;
        public uint    Index    => _index;
        public AlignmentData Alignment => _alignment;
        public ConnectionData Connection => _connection;
        public float4x4 TRS => float4x4.TRS(
            translation: Alignment.Offset, 
            rotation: quaternion.Euler(
                new float3(
                    x: 0.0f, 
                    y: math.radians(Alignment.Rotation), 
                    z: 0.0f)), 
            scale: Vector3.one);
        
        public RuntimeDescriptor(string identity, uint index)
        {
            _identity = identity;
            _index = index;
            _connection = new ConnectionData();
            _alignment = new AlignmentData();

            onChanged = delegate { };
        }

        public RuntimeDescriptor(RuntimeDescriptor runtimeDescriptor)
        {
            _identity   = runtimeDescriptor.Identity;
            _index      = runtimeDescriptor.Index;
            _alignment  = runtimeDescriptor._alignment;
            _connection = runtimeDescriptor._connection;
            onChanged   = delegate { };
        }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(_identity);
        }

        public RuntimeDescriptor AssignConnection(ConnectionData connectionData)
        {
            var copy = new RuntimeDescriptor(this);
            copy._connection = connectionData;
            return copy;
        }

        public RuntimeDescriptor AssignAlignment(AlignmentData alignment)
        {
            var copy = new RuntimeDescriptor(this);
            copy._alignment = alignment;
            return copy;
        }

        public RoomScriptableObject.BoundsInfo GetBounds(RoomScriptableObject scriptableObject, float4x4? trs, out int3 size)
        {
            var localTRS        = trs ?? TRS;
            var tileMatrix = scriptableObject.TileMatrix;

            (int3 min, int3 max) bounds = 
            (
                new int3(int.MaxValue, int.MaxValue, int.MaxValue),
                new int3(int.MinValue, int.MinValue, int.MinValue)
            );

            foreach (var tileDescription in tileMatrix)
            {
                var point = (int3)math.round(math.transform(localTRS, tileDescription.Value.Point));
                
                if (bounds.min.x > point.x) bounds.min.x = point.x;
                if (bounds.min.y > point.y) bounds.min.y = point.y;
                if (bounds.min.z > point.z) bounds.min.z = point.z;
                if (bounds.max.x < point.x) bounds.max.x = point.x;
                if (bounds.max.y < point.y) bounds.max.y = point.y;
                if (bounds.max.z < point.z) bounds.max.z = point.z;
            }
            
            size = new int3(
                x: math.abs(bounds.max.x - bounds.min.x),
                y: math.abs(bounds.max.y - bounds.min.y),
                z: math.abs(bounds.max.z - bounds.min.z)
            );

            return new RoomScriptableObject.BoundsInfo
            {
                Min = bounds.min,
                Max = bounds.max
            };
        }
    }
}