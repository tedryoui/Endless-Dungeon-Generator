using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Tools
{
    public class VisualLogicSeparatorWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Scripts/Editor/Tools/VisualLogicSeparatorWindow.uxml";
        private const string UssPath = "Assets/Scripts/Editor/Tools/VisualLogicSeparatorWindow.uss";

        private readonly List<GameObject> _targets = new();
        private ScrollView _targetsContainer;
        private Label _statusLabel;

        [MenuItem("Tools/Visual Logic Separator")]
        public static void Open()
        {
            var window = CreateInstance<VisualLogicSeparatorWindow>();
            window.titleContent = new GUIContent("Visual/Logic");
            window.minSize = new Vector2(420f, 300f);
            window.maxSize = new Vector2(700f, 650f);
            window.ShowModalUtility();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                rootVisualElement.Add(new Label($"Missing UXML: {UxmlPath}"));
                return;
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            visualTree.CloneTree(rootVisualElement);

            var addSelectedButton = rootVisualElement.Q<Button>("add-selected-button");
            var addSlotButton = rootVisualElement.Q<Button>("add-slot-button");
            var clearButton = rootVisualElement.Q<Button>("clear-button");
            var processButton = rootVisualElement.Q<Button>("process-button");

            _targetsContainer = rootVisualElement.Q<ScrollView>("targets-container");
            _statusLabel = rootVisualElement.Q<Label>("status-label");

            if (addSelectedButton == null ||
                addSlotButton == null ||
                clearButton == null ||
                processButton == null ||
                _targetsContainer == null ||
                _statusLabel == null)
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(new Label("UXML is invalid: missing required elements."));
                return;
            }

            addSelectedButton.clicked += AddSelectedObjects;
            addSlotButton.clicked += () =>
            {
                _targets.Add(null);
                RefreshTargetsList();
            };
            clearButton.clicked += () =>
            {
                _targets.Clear();
                RefreshTargetsList();
            };
            processButton.clicked += ProcessTargets;

            RefreshTargetsList();
        }

        private void AddSelectedObjects()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                SetStatus("Nothing selected.");
                return;
            }

            var added = 0;
            foreach (var go in selected)
            {
                if (go == null || EditorUtility.IsPersistent(go))
                {
                    continue;
                }

                if (_targets.Contains(go))
                {
                    continue;
                }

                if (!HasMeshComponent(go))
                {
                    continue;
                }

                _targets.Add(go);
                added++;
            }

            RefreshTargetsList();
            SetStatus(added > 0 ? $"Added: {added}" : "No valid objects were added.");
        }

        private void RefreshTargetsList()
        {
            _targetsContainer.Clear();

            if (_targets.Count == 0)
            {
                var emptyLabel = new Label("List is empty.");
                emptyLabel.AddToClassList("empty-list-label");
                _targetsContainer.Add(emptyLabel);
                return;
            }

            for (var i = 0; i < _targets.Count; i++)
            {
                var rowIndex = i;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var objectField = new ObjectField
                {
                    objectType = typeof(GameObject),
                    value = _targets[rowIndex]
                };
                objectField.style.flexGrow = 1;
                objectField.style.marginRight = 6;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    var go = evt.newValue as GameObject;
                    if (go != null && EditorUtility.IsPersistent(go))
                    {
                        objectField.SetValueWithoutNotify(_targets[rowIndex]);
                        SetStatus("Use scene objects only.");
                        return;
                    }

                    _targets[rowIndex] = go;
                });

                var removeButton = new Button(() =>
                {
                    _targets.RemoveAt(rowIndex);
                    RefreshTargetsList();
                })
                {
                    text = "X"
                };
                removeButton.style.width = 26;

                row.Add(objectField);
                row.Add(removeButton);
                _targetsContainer.Add(row);
            }
        }

        private void ProcessTargets()
        {
            if (_targets.Count == 0)
            {
                SetStatus("Nothing to process.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Separate Visual and Logic");
            var undoGroup = Undo.GetCurrentGroup();

            var processed = 0;
            var skipped = 0;

            foreach (var target in _targets)
            {
                if (!TryProcessTarget(target))
                {
                    skipped++;
                    continue;
                }

                processed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            SetStatus($"Processed: {processed}, skipped: {skipped}");
        }

        private static bool TryProcessTarget(GameObject target)
        {
            if (target == null || EditorUtility.IsPersistent(target) || !HasMeshComponent(target))
            {
                return false;
            }

            var oldParent = target.transform.parent;
            var oldSiblingIndex = target.transform.GetSiblingIndex();
            var sourceTag = target.tag;
            var sourceLayer = target.layer;

            var parentObject = new GameObject($"{target.name}_Logic");
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Logic Parent");

            var parentTransform = parentObject.transform;
            parentTransform.SetParent(oldParent, false);
            parentTransform.SetSiblingIndex(oldSiblingIndex);
            parentTransform.localPosition = target.transform.localPosition;
            parentTransform.localRotation = target.transform.localRotation;
            parentTransform.localScale = target.transform.localScale;
            parentObject.tag = sourceTag;
            parentObject.layer = sourceLayer;

            TransferNonVisualComponents(target, parentObject);

            Undo.SetTransformParent(target.transform, parentTransform, "Set Visual As Child");
            Undo.RecordObject(target, "Reset Visual Tag and Layer");
            Undo.RecordObject(target.transform, "Reset Visual Transform");
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            target.tag = "Untagged";
            target.layer = 0;

            EditorUtility.SetDirty(parentObject);
            EditorUtility.SetDirty(target);
            return true;
        }

        private static void TransferNonVisualComponents(GameObject source, GameObject destination)
        {
            var components = source.GetComponents<Component>();

            foreach (var component in components)
            {
                if (component == null ||
                    component is Transform ||
                    component is MeshRenderer ||
                    component is MeshFilter)
                {
                    continue;
                }

                ComponentUtility.CopyComponent(component);
                ComponentUtility.PasteComponentAsNew(destination);
                Undo.DestroyObjectImmediate(component);
            }
        }

        private static bool HasMeshComponent(GameObject go)
        {
            return go != null && (go.GetComponent<MeshRenderer>() != null || go.GetComponent<MeshFilter>() != null);
        }

        private void SetStatus(string message)
        {
            _statusLabel.text = message ?? string.Empty;
        }
    }
}
