using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace Core.Scripts.Helpers
{
    public enum UnityGameObjectTagEnum
    {
        Tile,
        Room
    }

    public static class UnityGameObjectTagFabric
    {
        public static string Get(UnityGameObjectTagEnum tag)
        {
            string stringInterpolation = tag switch
            {
                UnityGameObjectTagEnum.Tile => "Tile",
                UnityGameObjectTagEnum.Room => "Room",
                _                           => string.Empty
            };
            
#if UNITY_EDITOR
            if (!InternalEditorUtility.tags.Contains(stringInterpolation))
            {
                throw new Exception("Such tag: " + stringInterpolation + " not defined in Unity Registry!");
            }
#endif
            
            return stringInterpolation;
        }
    }
}