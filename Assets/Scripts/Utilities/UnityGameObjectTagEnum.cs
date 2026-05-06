using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace Core.Scripts.Helpers
{
    public enum UnityGameObjectTagEnum
    {
        Tile
    }

    public static class UnityGameObjectTagFabric
    {
        public static string Get(UnityGameObjectTagEnum tag)
        {
            string stringInterpolation = "";
            
            switch (tag)
            {
                case UnityGameObjectTagEnum.Tile:
                    stringInterpolation = "Tile";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
            }
            
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