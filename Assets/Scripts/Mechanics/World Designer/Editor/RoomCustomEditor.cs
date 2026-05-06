using Mechanics.World_Designer.Gizmos;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mechanics.World_Designer.Editor
{
    [CustomEditor(typeof(Room))]
    public class RoomCustomEditor : UnityEditor.Editor
    {
        public static bool DrawScheme = false;
        
        private Button _schemeButton;
        
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            InspectorElement.FillDefaultInspector(container, serializedObject, this);

            AddSchemeButton(container);
            
            container.TrackSerializedObjectValue(serializedObject, o =>
            {
                if (target is Room room)
                {
                    room.UpdateData();
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            });
            
            return container;
        }

        private void AddSchemeButton(VisualElement container)
        {
            _schemeButton = new Button()
            {
                text = "Draw Scheme"
            };

            _schemeButton.clicked += OnSchemeButtonClicked;
            UpdateSchemeButtonStyle();
            
            container.Add(_schemeButton);
        }

        private void OnSchemeButtonClicked()
        {
            DrawScheme = !DrawScheme;
            UpdateSchemeButtonStyle();
            
            var targetGameObject = (target as Room)?.gameObject;
            if (targetGameObject == null) return;
            
            if (DrawScheme == false)
            {
                if (SceneVisibilityManager.instance.IsHidden(targetGameObject.transform.GetChild(0).gameObject))
                    SceneVisibilityManager.instance.Show(targetGameObject, true);
                
                if (targetGameObject.TryGetComponent(out RoomGizmoDrawer drawer1))
                    DestroyImmediate(drawer1);

                return;
            };

            if (!SceneVisibilityManager.instance.IsHidden(targetGameObject.transform.GetChild(0).gameObject))
            {
                SceneVisibilityManager.instance.Hide(targetGameObject, true);
                SceneVisibilityManager.instance.Show(targetGameObject, false);
            }
            
            if (!targetGameObject.TryGetComponent(out RoomGizmoDrawer drawer2))
                targetGameObject.AddComponent<RoomGizmoDrawer>();
                
        }

        private void UpdateSchemeButtonStyle()
        {
            var isSchemeEnabled = DrawScheme;
            
            if (isSchemeEnabled)
            {
                _schemeButton.text                  = $"[ENABLED] Draw Scheme";
                _schemeButton.style.opacity         = new StyleFloat(0.4f);
            }
            else
            {
                _schemeButton.text                  = $"[DISABLED] Draw Scheme";
                _schemeButton.style.opacity         = new StyleFloat(1.0f);
            }
        }
    }
}