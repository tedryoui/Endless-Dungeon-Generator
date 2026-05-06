using Unity.Mathematics;
using UnityEngine;

namespace Extensions
{
    public static class QuaternionExtension
    {
        /// <summary>
        /// Конвертирует Unity.Mathematics.quaternion в UnityEngine.Quaternion
        /// </summary>
        public static Quaternion ToUnity(this quaternion q)
        {
            return new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w);
        }

        /// <summary>
        /// Конвертирует UnityEngine.Quaternion в Unity.Mathematics.quaternion
        /// </summary>
        public static quaternion ToMathematics(this Quaternion q)
        {
            return new quaternion(q.x, q.y, q.z, q.w);
        }
    }
}