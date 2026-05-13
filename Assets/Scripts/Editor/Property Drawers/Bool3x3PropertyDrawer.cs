using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Property_Drawers
{
    [CustomPropertyDrawer(typeof(bool3x3))]
    public class Bool3x3PropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Root container
            var root = new VisualElement();
            root.style.marginBottom = 4;

            // Property Label
            var label = new Label(property.displayName);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(label);

            // Grid Container
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.paddingLeft   = 15; // Indent to match Unity's Inspector style

            // Unity.Mathematics matrices are Column-Major (c0, c1, c2)
            string[] columns = { "c0", "c1", "c2" };
            string[] rows    = { "x", "y", "z" };

            foreach (var colName in columns)
            {
                var columnContainer = new VisualElement();
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.marginRight   = 10;

                var colProp = property.FindPropertyRelative(colName);

                foreach (var rowName in rows)
                {
                    var elementProp = colProp.FindPropertyRelative(rowName);
                
                    if (elementProp != null)
                    {
                        var toggle = new Toggle();
                        toggle.BindProperty(elementProp);
                    
                        // Compact styling
                        toggle.style.width  = 18;
                        toggle.style.height = 18;
                    
                        columnContainer.Add(toggle);
                    }
                }
                grid.Add(columnContainer);
            }

            root.Add(grid);
            return root;
        }
    }
}