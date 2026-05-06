using System;
using Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.Property_Drawers
{
    [CustomPropertyDrawer(typeof(ListPropertyAttribute))]
    public class ListPropertyAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            ListPropertyAttribute listAttribute = (ListPropertyAttribute)attribute;
            
            if (listAttribute.IsFolded)
                property.isExpanded = true;

            PropertyField propertyField = new PropertyField(property);
            
            return propertyField;
        }
    }
}