using System;
using System.Security;
using Attributes;
using Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer.Gizmos
{
    [ExecuteInEditMode]
    public class RoomBuilderUtility : MonoBehaviour
    {
        [SerializeField, DisabledProperty] private RoomScriptableObject _scriptableObject;
        [SerializeField, DisabledProperty] private bool                 _isEnabled;
        [SerializeField, DisabledProperty] private bool                 _needToRefresh;

        [SerializeField] private Grid _grid;
        
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public bool NeedToRefresh
        {
            get => _needToRefresh;
            set => _needToRefresh = value;
        }

        public void SetScriptableObject(RoomScriptableObject scriptableObject)
        {
            _scriptableObject = scriptableObject;
            NeedToRefresh = true;
        }

        private void OnDrawGizmos()
        {
            if (_scriptableObject == null) return;
            if (!_isEnabled) return;
            
            if (_needToRefresh)
                UpdateParameters();

            DrawZeroPoint();
        }

        private void DrawZeroPoint()
        {
            var worldPoint = _grid.ToWorld(new int3(0, 0, 0));

            UnityEngine.Gizmos.color = Color.white;
            UnityEngine.Gizmos.DrawSphere(worldPoint, 0.1f);
        }

        private void UpdateParameters()
        {
            _needToRefresh = false;
            
            _grid = Grid.Create(
                new float3(1, 1, 1), 
                Grid.GridMode.Local, 
                transform.position, 
                transform.rotation.ToMathematics());
        }
    }
}