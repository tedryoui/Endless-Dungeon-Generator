using UnityEngine;

namespace Core.Scripts.Helpers
{
    public class Pivot
    {
        private Transform _transform;
        private Vector3   _offset; // Локальное смещение пивота

        public Pivot(Transform transform)
        {
            _transform = transform;
            _offset    = Vector3.zero;
        }

        /// <summary>
        /// Устанавливает локальную точку, которая будет считаться "центром" (пивотом)
        /// </summary>
        public void SetPivotTo(Vector3 localValue)
        {
            _offset = localValue;
        }

        /// <summary>
        /// Устанавливает позицию объекта так, чтобы точка _offset оказалась в мировых координатах value
        /// </summary>
        public void SetPosition(Vector3 value)
        {
            _transform.position = value - _offset;
        }
    }
}