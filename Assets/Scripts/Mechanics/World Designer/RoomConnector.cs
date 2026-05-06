using System;
using System.Collections.Extension;
using System.Linq;
using Attributes;
using Service;
using Service.Concrete;
using UnityEngine;

namespace Mechanics.World_Designer
{
    public class RoomConnector : MonoBehaviour
    {
        private TreeNode<RoomStructure> _node;

        [SerializeField, DisabledProperty] private uint            _nodeIndex;
        [SerializeField]                   private uint            _exitIndex;
        [SerializeField]                   private RoomDescription _nextRoomDescription;

        public TreeNode<RoomStructure> Node
        {
            get => _node;
            set
            {
                _node      = value;
                _nodeIndex = _node.Value.NodeIndex;
            }
        }

        public void BuildNextRoom()
        {
            if (_node.Children.Any(x => x.Value.ParentExitIndex.Equals(_exitIndex)))
                return;
            
            var designService = ServiceLocator.Instance.GetService<WorldDesignService>(new ServiceLocator.ServiceIdentity()
            {
                ID = "World Design Service"
            });

            var roomDescription = new RoomStructure(
                designService.NextNodeIndex, 
                _nodeIndex, 
                _exitIndex, 
                _nextRoomDescription);

            designService.Place(roomDescription, _node);
        }
    }
}