using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Service.Concrete.Editor
{
    [CustomEditor(typeof(WorldDesignService))]
    public class WorldDesignServiceCustomEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var content = new VisualElement();
            InspectorElement.FillDefaultInspector(content, serializedObject, this);

            {
                var parentUIntIntegerField = new IntegerField()
                {
                    label = "Parent Node Index"
                };

                var identityTextField = new TextField()
                {
                    label = "Identity"
                };

                var exitIndexIntegerField = new IntegerField()
                {
                    label = "Exit Index"
                };

                var button = new Button()
                {
                    text = "Add",
                    clickable = new Clickable(() =>
                    {
                        var wds             = (target as WorldDesignService);
                        var parentNodeIndex = (uint)parentUIntIntegerField.value;
                        var identity        = identityTextField.value;
                        var exitIndex       = exitIndexIntegerField.value;

                        wds.Add(parentNodeIndex, identity, exitIndex);
                    })
                };
                
                var section = new VisualElement()
                {
                    style =
                    {
                        display = new StyleEnum<DisplayStyle>(EditorApplication.isPlaying ? DisplayStyle.Flex : DisplayStyle.None),
                        marginTop = new StyleLength(Length.Pixels(18))
                    }
                };
                section.Add(parentUIntIntegerField);
                section.Add(identityTextField);
                section.Add(exitIndexIntegerField);
                section.Add(button);
                content.Add(section);
            }
            
            return content;
        }
    }
}