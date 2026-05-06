using System;
using System.Linq;
using Attributes;
using Extensions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Mechanics.World_Designer
{
    public class Room : MonoBehaviour
    {
        [Serializable]
        public struct RoomBounds
        {
            public float3 Center;
            
            public float Width;
            public float Height;
            public float Depth;

            public int3 Min;
            public int3 Max;
        }

        [Serializable]
        public struct RoomInfo
        {
            public enum Direction { Forward, Backward, Left, Right }
            
            public bool HasEnter;
            public int ExitCount; 

            public int3 Enter;
            public Direction EnterDirection;
            [ListProperty(true)] public int3[] Exits;
            [ListProperty(true)] public Direction[] ExitDirections;
        }

        [SerializeField, DisabledProperty] private Collider _areaCollider;
        
        [Space(20)]
        [SerializeField, DisabledProperty] private RoomBounds _roomBounds;
        [Space]
        [SerializeField, DisabledProperty] private RoomInfo _roomInfo;

        public RoomBounds Bounds
        {
            get => _roomBounds;
            set => _roomBounds = value;
        }

        public RoomInfo Info
        {
            get => _roomInfo;
            set => _roomInfo = value;
        }
        
        private LayerMask AreaColliderLayerMask => LayerMask.GetMask("Default");

        private void Awake()
        {
            var areaBoxCollider = gameObject.AddComponent<BoxCollider>();
            areaBoxCollider.isTrigger     = true;
            areaBoxCollider.includeLayers = AreaColliderLayerMask;
            areaBoxCollider.center        = transform.GetSceneCenter();
            areaBoxCollider.size          = new Vector3(Bounds.Width + 6.0f, 1.0f, Bounds.Depth + 6.0f);

            _areaCollider = areaBoxCollider;
        }

        [ContextMenu("Add Room Collider")]
        public void AddRoomCollider()
        {
            var areaBoxCollider = gameObject.AddComponent<BoxCollider>();
            areaBoxCollider.isTrigger     = true;
            areaBoxCollider.includeLayers = AreaColliderLayerMask;
            areaBoxCollider.center        = transform.GetSceneCenter();
            areaBoxCollider.size          = new Vector3(Bounds.Width + 6.0f, 1.0f, Bounds.Depth + 6.0f);

            _areaCollider = areaBoxCollider;
        }

        public Grid BuildGrid
        {
            get
            {
                var grid = Grid.Create(new float3(1), Grid.GridMode.World); 
                
                return grid;
            }
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                UpdateData();
            }
        }

        public void UpdateData()
        {
            RoomResolver resolver = 
                new RoomResolver(this);
            
            resolver.Resolve();
        }

        public bool IsValidPlacement(out Collider[] obstacles, Vector3? checkPosition = null)
        {
            var sceneCenter = checkPosition ?? Bounds.SceneCenter;
            var size        = new Vector3(Bounds.Width, 1.0f, Bounds.Depth) + new Vector3(6.0f, 0.0f, 6.0f);

            obstacles = Physics.OverlapBox(
                sceneCenter, 
                size / 2.0f, 
                Quaternion.identity, 
                AreaColliderLayerMask, 
                QueryTriggerInteraction.Collide);

            obstacles = obstacles.Where(x => x.gameObject.CompareTag("Room") && x.gameObject != gameObject).ToArray();
            
            return obstacles.Length == 0;
        }
    }
}
