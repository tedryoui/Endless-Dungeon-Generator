using Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor_Scripts.Property_Drawers
{
    [CustomPropertyDrawer(typeof(InlinePropertyAttribute))]
    public class InlinePropertyAttributePropertyDrawer: PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            
            container.Add(new Label(property.displayName)
            {
                style = { marginLeft = new StyleLength(3)}
            });

            if (property.hasChildren)
            {
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();

                iterator.NextVisible(true);

                while (!SerializedProperty.EqualContents(iterator, endProperty))
                {
                    var field = new PropertyField(iterator.Copy());
                
                    field.BindProperty(iterator);
                    container.Add(field);

                    iterator.NextVisible(false);
                }
            }
            else
            {
                container.Add(new PropertyField(property));
            }

            return container;
        }
    }
}