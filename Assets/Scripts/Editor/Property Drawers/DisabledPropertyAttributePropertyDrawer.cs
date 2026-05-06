using Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor_Scripts.Property_Drawers
{
    [CustomPropertyDrawer(typeof(DisabledPropertyAttribute))]
    public class DisabledPropertyAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            PropertyField propertyField = new PropertyField(property);
            propertyField.SetEnabled(false);
            
            return propertyField;
        }
    }
}