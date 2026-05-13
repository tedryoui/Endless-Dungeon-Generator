using System;
using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "Tunnel", menuName = "World Design/Tunnel", order = 0)]
    public class TunnelScriptableObject : ScriptableObject
    {
        [Serializable]
        public struct Rule
        {
            public bool3x3              Matrix;
            public TileScriptableObject Tile;
        }
        
        [SerializeField] private string _identity;
        [SerializeField] private Rule[] _rules;
        
        public string Identity => _identity;
        public Rule[] Rules => _rules;
    }
}