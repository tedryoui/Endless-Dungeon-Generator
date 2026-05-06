using System;
using UnityEngine;

namespace Core.Scripts.Helpers
{
    public class PivotUtility : MonoBehaviour
    {
        [SerializeField] private Vector3 _pivotPoint;
        [SerializeField] private Vector3 _position;
        
        public void Apply()
        {
            var pivot = new Pivot(transform);
            pivot.SetPivotTo(_pivotPoint);
            pivot.SetPosition(_position);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position + _pivotPoint, new Vector3(0.3f, 5f, 0.3f));
            
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(_position, new Vector3(0.3f, 5f, 0.3f));
        }
    }
}