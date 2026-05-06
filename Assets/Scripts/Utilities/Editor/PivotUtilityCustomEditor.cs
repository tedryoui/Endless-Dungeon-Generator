using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Core.Scripts.Helpers.Editor
{
    [CustomEditor(typeof(PivotUtility))]
    public class PivotUtilityCustomEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var content = new VisualElement();
            InspectorElement.FillDefaultInspector(content, serializedObject, this);
            
            var button = new Button();
            button.text = "Apply";

            button.clicked += (target as PivotUtility).Apply;
            
            content.Add(button);
            
            return content;
        }
    }
}