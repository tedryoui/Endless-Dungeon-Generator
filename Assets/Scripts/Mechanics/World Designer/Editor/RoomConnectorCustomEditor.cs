using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Mechanics.World_Designer.Editor
{
    [CustomEditor(typeof(RoomConnector))]
    public class RoomConnectorCustomEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var content = new VisualElement();
            InspectorElement.FillDefaultInspector(content, serializedObject, this);

            var button = new Button()
            {
                text = "Place"
            };

            button.clicked += Place;
            
            content.Add(button);
            
            return content;
        }

        private void Place()
        {
            var connector = target as RoomConnector;

            if (connector != null)
            {
                connector.BuildNextRoom();
            }
        }
    }
}