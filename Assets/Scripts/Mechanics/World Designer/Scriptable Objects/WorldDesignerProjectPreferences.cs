using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Mechanics.World_Designer
{
    [CreateAssetMenu(fileName = "World Design Project Preferences", menuName = "World Design/Set", order = 0)]
    public class WorldDesignerProjectPreferences : ScriptableObject
    {
        [SerializeField] private List<RoomScriptableObject>   _roomScriptableObjects;
        [SerializeField] private List<TileScriptableObject>   _tileScriptableObjects;
        [SerializeField] private List<TunnelScriptableObject> _tunnelScriptableObjects;

        public ReadOnlyCollection<RoomScriptableObject>   Rooms   => _roomScriptableObjects.AsReadOnly();
        public ReadOnlyCollection<TileScriptableObject>   Tiles   => _tileScriptableObjects.AsReadOnly();
        public ReadOnlyCollection<TunnelScriptableObject> Tunnels => _tunnelScriptableObjects.AsReadOnly();
        
        #if UNITY_EDITOR
        
        private static WorldDesignerProjectPreferences _editorInstance;

        public static WorldDesignerProjectPreferences EditorInstance
        {
            get
            {
                if (_editorInstance == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(WorldDesignerProjectPreferences)}");
                
                    if (guids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _editorInstance = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldDesignerProjectPreferences>(path);
                    }
                
                    if (_editorInstance == null)
                    {
                        _editorInstance = CreateInstance<WorldDesignerProjectPreferences>();
                        var path = "Assets/WorldDesignerProjectPreferences.asset";
                        UnityEditor.AssetDatabase.CreateAsset(_editorInstance, path);
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                }
                return _editorInstance;
            }
        }
        
        #endif
    }
}