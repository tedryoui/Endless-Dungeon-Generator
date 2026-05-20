using Mechanics.World_Designer.Gizmos;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Mechanics.World_Designer.Editor
{
    [CustomEditor(typeof(RoomBuilderUtility))]
    public class RoomBuilderUtilityCustomEditor : UnityEditor.Editor
    {
        private RoomBuilderUtility _roomBuilderUtility;
        private RoomBuilderUtility RoomBuilderUtility => _roomBuilderUtility ??= target as RoomBuilderUtility;
        
        private Button _enableButton;
        
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            var baseObject              = serializedObject.FindProperty("_scriptableObject");
            var baseObjectPropertyField = new PropertyField(baseObject)
            {
                style =
                {
                    marginBottom = new StyleLength(18)
                }
            };

            _enableButton = new Button()
            {
                text      = RoomBuilderUtility.IsEnabled ? "Disable" : "Enable",
                clickable = new Clickable(OnEnableButtonClicked)
            };

            container.Add(baseObjectPropertyField);
            container.Add(_enableButton);

            return container;
        }

        private void OnEnableButtonClicked(EventBase obj)
        {
            RoomBuilderUtility.IsEnabled = !RoomBuilderUtility.IsEnabled;
            _enableButton.text = RoomBuilderUtility.IsEnabled ? "Disable" : "Enable";
        }
    }
}