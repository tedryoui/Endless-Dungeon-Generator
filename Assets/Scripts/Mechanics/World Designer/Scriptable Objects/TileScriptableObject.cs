using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Mathematics;
using UnityEngine;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "Tile", menuName = "World Design/Tile", order = 1)]
    public class TileScriptableObject : ScriptableObject
    {
        public enum TileType { Void, Floor, Wall }
        
        [SerializeField] private string          _identity;
        [SerializeField] private GameObject      _prefab;
        [SerializeField] private TileType        _type;
        [SerializeField] private List<Direction> _outputs;
        
        public string                        Identity => _identity;
        public GameObject                    Prefab   => _prefab;
        public TileType                      Type     => _type;
        public ReadOnlyCollection<Direction> Outputs  => _outputs.AsReadOnly();
    }
}