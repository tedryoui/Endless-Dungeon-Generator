using System;
using Attributes;
using Service.Concrete;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mechanics.World_Designer
{
    [Serializable]
    public struct RoomStructure
    {
        [Serializable]
        public struct RoomTunnel
        {
            [SerializeField, DisabledProperty] private int3   _exitGridIndex;
            [SerializeField, DisabledProperty] private int3   _enterGridIndex;
            [SerializeField, DisabledProperty] private int3[] _pathGridIndices;

            [SerializeField, DisabledProperty] private Tunnel _tunnel;

            public     int3 ExitGridIndex => _exitGridIndex;
            public     int3 EnterGridIndex => _enterGridIndex;
            public     int3[] PathGridIndices => _pathGridIndices;
            
            public RoomTunnel(int3 exitGridIndex, int3 enterGridIndex, int3[] pathGridIndices, Tunnel tunnel)
            {
                _exitGridIndex   = exitGridIndex;
                _enterGridIndex  = enterGridIndex;
                _pathGridIndices = pathGridIndices;
                _tunnel          = tunnel;
            }
        }

        [SerializeField, DisabledProperty] private uint       _parentNodeIndex;
        [SerializeField, DisabledProperty] private uint       _nodeIndex;
        [SerializeField, DisabledProperty] private RoomTunnel _tunnel;
        [SerializeField, DisabledProperty] private uint       _parentExitIndex;
        [SerializeField, DisabledProperty] private string     _identity;
        [SerializeField, DisabledProperty] private RoomType   _type;
        
        private Room _room;
        
        public uint     NodeIndex => _nodeIndex;
        public uint     ParentExitIndex => _parentExitIndex;
        public string   Identity  => _identity;
        public RoomType Type      => _type;
        public bool     IsValid   => Type is not RoomType.None;
        
        public Room Room => _room;

        public RoomStructure(uint nodeIndex, uint parentNodeIndex, uint parentExitIndex, RoomDescription description)
        { 
            _nodeIndex       = nodeIndex;
            _parentNodeIndex = parentNodeIndex;
            _parentExitIndex = parentExitIndex;
            _identity        = description.Identity;
            _type            = description.Type;
            
            _room   = null;
            _tunnel = default;
        }

        private RoomStructure(RoomStructure structure)
        {
            _nodeIndex       = structure.NodeIndex;
            _parentExitIndex = structure.ParentExitIndex;
            _identity        = structure.Identity;
            _type            = structure.Type;
            _room            = structure.Room;
            _parentNodeIndex = structure._parentNodeIndex;
            _tunnel          = structure._tunnel;
        }

        public RoomStructure AssignRoom(Room room)
        {
            var copy = new RoomStructure(this);
            copy._room = room;

            return copy;
        }

        public RoomStructure AssignTunnel(RoomTunnel tunnel)
        {
            var copy = new RoomStructure(this);
            copy._tunnel = tunnel;

            return copy;
        }
    }
}