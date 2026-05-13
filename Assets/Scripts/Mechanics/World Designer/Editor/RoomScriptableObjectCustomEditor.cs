using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.Scripts.Helpers;
using Extensions;
using Mechanics.World_Designer.Gizmos;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Label = Core.Scripts.Helpers.Label;

namespace Mechanics.World_Designer.Editor
{
    [CustomEditor(typeof(RoomScriptableObject))]
    public class RoomScriptableObjectCustomEditor : UnityEditor.Editor
    {
        private RoomScriptableObject _scriptableObject;
        private RoomScriptableObject ScriptableObject => _scriptableObject ??=  target as RoomScriptableObject;
        
        private static RoomBuilderUtility _builderUtility;
        
        private Button _saveButton;
        private Button _editButton;
        private Button _clearButton;

        private Button        _enterSnapshotModeButton;
        private DropdownField _snapshotPreviewDropdownField;
        private Button        _takeSnapshotButton;
        private TextField     _takeSnapshotIdentityTextField;
        private Button        _deleteSnapshotButton;
        private IntegerField  _deleteSnapshotIndexIntegerField;

        private static CancellationTokenSource _cancellationTokenSource;
        
        public override VisualElement CreateInspectorGUI()
        {
            var content = new VisualElement();
            InspectorElement.FillDefaultInspector(content, serializedObject, this);

            _saveButton = new Button()
            {
                text = "Save",
                style =
                {
                    flexGrow = 1,
                    display = new StyleEnum<DisplayStyle>(_previewScene.IsValid() ? DisplayStyle.Flex : DisplayStyle.None),
                }
            };
            _saveButton.clicked += OnSaveButtonClicked;
            
            _editButton = new Button()
            {
                text = "Edit",
                style =
                {
                    flexGrow = 1,
                    display = new StyleEnum<DisplayStyle>(!_previewScene.IsValid() ? DisplayStyle.Flex : DisplayStyle.None)
                }
            };
            _editButton.clicked += OnEditButtonClicked;

            _clearButton = new Button()
            {
                text = "Clear",
                style =
                {
                    flexGrow = 1,
                },
            };
            _clearButton.clicked += OnClearButtonClicked;

            var buttonSection = new VisualElement()
            {
                style =
                {
                    flexDirection =  FlexDirection.Row,
                    width = new StyleLength(Length.Percent(100)),
                    height    = 25,
                    marginTop = 10,
                }
            };
            
            buttonSection.Add(_saveButton);
            buttonSection.Add(_editButton);
            buttonSection.Add(_clearButton);

            _enterSnapshotModeButton = new Button()
            {
                text = _editMode switch
                {
                    EditMode.Freeze or EditMode.Default  => "ENTER SNAPSHOT MODE",
                    EditMode.Freeze or EditMode.Snapshot => "EXIT SNAPSHOT MODE",
                    _                                    => throw new ArgumentOutOfRangeException()
                },
                style =
                {
                    height       = new StyleLength(Length.Pixels(32)),
                    marginBottom = new StyleLength(9)
                },
                clickable = new Clickable(OnEnterSnapshotModeButtonClicked)
            };

            _snapshotPreviewDropdownField = new DropdownField()
            {
                value = "NONE",
                choices = new List<string>(new[] { "NONE" }).Concat(ScriptableObject.Snapshots.Select(x => x.Identity))
                    .ToList(),
                style =
                {
                    display = _editMode switch
                    {
                        EditMode.Freeze or EditMode.Default                     => new StyleEnum<DisplayStyle>(DisplayStyle.None),
                        EditMode.Freeze or EditMode.Snapshot => new StyleEnum<DisplayStyle>(DisplayStyle.Flex),
                        _                                    => throw new ArgumentOutOfRangeException()
                    }
                }
            };
            _snapshotPreviewDropdownField.RegisterValueChangedCallback(OnSnapshotPreviewChanged);

            _takeSnapshotButton = new Button()
            {
                text      = "Take Snapshot",
                clickable = new Clickable(OnTakeSnapshotButtonClicked),
                style =
                {
                    width = new StyleLength(Length.Pixels(150)),
                    flexGrow = new StyleFloat(0.0f)
                }
            };

            _takeSnapshotIdentityTextField = new TextField()
            {
                value = "IDENTITY HERE...",
                style =
                {
                    flexGrow = new StyleFloat(1.0f)
                }
            };

            var takeSnapshotSection = new VisualElement()
            {
                style =
                {
                    display = _editMode switch
                    {
                        EditMode.Freeze or EditMode.Default  => new StyleEnum<DisplayStyle>(DisplayStyle.None),
                        EditMode.Freeze or EditMode.Snapshot => new StyleEnum<DisplayStyle>(DisplayStyle.Flex),
                        _                                    => throw new ArgumentOutOfRangeException()
                    },
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    width         = new StyleLength(Length.Percent(100)),
                }
            };

            _deleteSnapshotButton = new Button()
            {
                text      = "Delete Snapshot",
                clickable = new Clickable(OnDeleteSnapshotButtonClicked),
                style =
                {
                    width = new StyleLength(Length.Pixels(150)),
                    flexGrow = new StyleFloat(0.0f)
                }
            };

            _deleteSnapshotIndexIntegerField = new IntegerField()
            {
                label = "",
                style =
                {
                    flexGrow = new StyleFloat(1.0f)
                }
            };

            var deleteSnapshotSection = new VisualElement()
            {
                style =
                {
                    display = _editMode switch
                    {
                        EditMode.Freeze or EditMode.Default  => new StyleEnum<DisplayStyle>(DisplayStyle.None),
                        EditMode.Freeze or EditMode.Snapshot => new StyleEnum<DisplayStyle>(DisplayStyle.Flex),
                        _                                    => throw new ArgumentOutOfRangeException()
                    },
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    width         = new StyleLength(Length.Percent(100)),
                }
            };

            var snapshotSection = new VisualElement()
            {
                style =
                {
                    display = new StyleEnum<DisplayStyle>(_previewScene.IsValid()
                        ? DisplayStyle.Flex
                        : DisplayStyle.None),
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column),
                    marginTop     = new StyleLength(18)
                }
            };
            
            takeSnapshotSection.Add(_takeSnapshotIdentityTextField);
            takeSnapshotSection.Add(_takeSnapshotButton);
            
            deleteSnapshotSection.Add(_deleteSnapshotButton);
            deleteSnapshotSection.Add(_deleteSnapshotIndexIntegerField);
            
            snapshotSection.Add(_enterSnapshotModeButton);
            snapshotSection.Add(_snapshotPreviewDropdownField);
            snapshotSection.Add(takeSnapshotSection);
            snapshotSection.Add(deleteSnapshotSection);
            
            content.Add(buttonSection);
            content.Add(snapshotSection);
            
            return content;
        }

        private void OnSnapshotPreviewChanged(ChangeEvent<string> evt)
        {
            ReconstructTileMatrix();

            async void ReconstructSnapshotAsync(string value, CancellationToken cancellationToken)
            {
                await Task.Delay(1000, cancellationToken);
                
                if (!value.Equals("NONE"))
                {
                    var snapshot = ScriptableObject.Snapshots.First(x => x.Identity.Equals(value));
                
                    ReconstructSnapshot(snapshot);
                }
            };
            
            ReconstructSnapshotAsync(evt.newValue, _cancellationTokenSource.Token);
        }

        private void OnEnterSnapshotModeButtonClicked(EventBase obj)
        {
            _editMode = _editMode switch
            {
                EditMode.Freeze or EditMode.Default  => EditMode.Snapshot,
                EditMode.Freeze or EditMode.Snapshot => EditMode.Default,
                _                                    => throw new ArgumentOutOfRangeException()
            };
            
            ReconstructTileMatrix();

            async void RefreshDataAsync(CancellationToken cancellationToken)
            {
                await Task.Delay(250, cancellationToken);
                
                RefreshData();
            }
            
            RefreshDataAsync(_cancellationTokenSource.Token);
        }

        private void OnClearButtonClicked()
        {
            if (!EditorUtility.DisplayDialog("Confirmation!", "Are you sure you want to clear data!", "Yes", "No"))
                return;
            
            ScriptableObject.Clear();
        }

        private void OnEditButtonClicked()
        {
            OpenEditScene();
        }

        private void OnSaveButtonClicked()
        {
            if (_previewScene.IsValid())
                CloseEditScene();
        }

        private void OnTakeSnapshotButtonClicked(EventBase obj)
        {
            TakeSnapshot();
        }

        private void OnDeleteSnapshotButtonClicked(EventBase obj)
        {
            var value = _deleteSnapshotIndexIntegerField.value;
            
            if (value < 0 || value >= ScriptableObject.Snapshots.Count) 
                throw new Exception($"Snapshot index {value} is out of range.");

            RemoveSnapshot(value);
        }

        #region Edit Scene

        private enum EditMode { Freeze, Default, Snapshot }

        private static EditMode _editMode;

        private static string _previousScenePath;

        private static Scene _previewScene;

        private void OpenEditScene()
        {
            _editMode          = EditMode.Default;
            _previousScenePath = EditorSceneManager.GetActiveScene().path;
            _previewScene      = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(_previewScene);
            
            EditorUtility.OpenPropertyEditor(ScriptableObject);

            ObjectChangeEvents.changesPublished             += OnChangesPublished;
            EditorSceneManager.activeSceneChangedInEditMode += OnExternalClose;

            _cancellationTokenSource = new CancellationTokenSource();
            
            SetupPreviewSceneBasicHierarchy();
        }

        private void CloseEditScene()
        {
            EditorSceneManager.OpenScene(_previousScenePath);
            
            OnExternalClose(default, default);
        }

        private void OnExternalClose(Scene arg0, Scene arg1)
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;

            _cancellationTokenSource?.Cancel();
            
            _previousScenePath       = string.Empty;
            _previewScene            = default;
            _editMode                = EditMode.Default;
            _builderUtility          = null;
            _cancellationTokenSource = null;
        }

        private void SetupPreviewSceneBasicHierarchy()
        {
            if (!_previewScene.IsValid()) return;

            var rootGameObject    = new GameObject(ScriptableObject.Identity);
            
            var basicViewGameObject = new GameObject("Basic View");
            basicViewGameObject.transform.SetParent(rootGameObject.transform);
            
            _builderUtility = rootGameObject.AddComponent<RoomBuilderUtility>();
            _builderUtility.SetScriptableObject(ScriptableObject);
            
            ReconstructTileMatrix();

            async void RefreshAsync(CancellationToken cancellationToken)
            {
                await Task.Delay(1000, cancellationToken);
                RefreshData();
            }
            
            RefreshAsync(_cancellationTokenSource.Token);
        }

        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            _builderUtility.NeedToRefresh = true;
            
            if (_editMode == EditMode.Default)
                RefreshData();
        }

        #endregion

        #region Data

        private void RefreshData()
        {
            RefreshTileMatrix();
            RefreshBounds();
            RefreshGates();
        }

        private void RefreshGates()
        {
            var rootGameObject = _builderUtility.gameObject;
            var basicView      = rootGameObject.transform.Find("Basic View");

            var basicViewChildren = basicView.transform.GetChildren(1);
            var tag               = UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Tile);

            var grid = Grid.Create(
                new float3(1, 1, 1), 
                Grid.GridMode.Local, 
                basicView.position,
                basicView.rotation.ToMathematics());
            
            var enterGates = new List<GameObject>();
            var exitGates = new List<GameObject>();
            
            foreach (var basicViewChild in basicViewChildren)
            {
                if (basicViewChild.CompareTag(tag))
                {
                    if (basicViewChild.TryGetComponent<Enter>(out _))
                        enterGates.Add(basicViewChild.gameObject);
                    else if (basicViewChild.TryGetComponent<Exit>(out _))
                        exitGates.Add(basicViewChild.gameObject);
                }
            }

            var exitGateInfos = new List<RoomScriptableObject.GateInformation>();
            
            foreach (var exitGate in exitGates)
            {
                var exit      = exitGate.gameObject.GetComponent<Exit>();
                var point     = grid.ToGrid(exitGate.transform.position);
                var direction = exit.Direction;
                var start     = exit.Start;
                var snapshot  = exit.SnapshotIdentity;
                var tunnel    = exit.TunnelIdentity;

                exitGateInfos.Add(new RoomScriptableObject.GateInformation
                {
                    Point            = point,
                    Direction        = direction,
                    SnapshotIdentity = snapshot,
                    Pivot            = start,
                    TunnelIdentity   = tunnel
                });
            }
            
            var enterGateInfos = new List<RoomScriptableObject.GateInformation>();
            
            foreach (var enterGate in enterGates)
            {
                var exit      = enterGate.gameObject.GetComponent<Enter>();
                var point     = grid.ToGrid(enterGate.transform.position);
                var direction = exit.Direction;
                var start     = exit.End;
                var snapshot  = exit.SnapshotIdentity;
                var tunnel    = exit.TunnelIdentity;

                enterGateInfos.Add(new RoomScriptableObject.GateInformation
                {
                    Point            = point,
                    Direction        = direction,
                    SnapshotIdentity = snapshot,
                    Pivot            = start,
                    TunnelIdentity   = tunnel
                });
            }
            
            ReflectionHelper.SetField(ScriptableObject, "_enterGates", enterGateInfos);
            ReflectionHelper.SetField(ScriptableObject, "_exitGates", exitGateInfos);
        }

        private void RefreshBounds()
        {
            var                         tileMatrix = ScriptableObject.TileMatrix;
            (int3 min, int3 max) bounds = (new int3(int.MaxValue, int.MaxValue, int.MaxValue),
                new int3(int.MinValue,                            int.MinValue, int.MinValue));

            foreach (var tileDescription in tileMatrix)
            {
                var point = tileDescription.Value.Point;
                
                if (bounds.min.x > point.x) bounds.min.x = point.x;
                if (bounds.min.y > point.y) bounds.min.y = point.y;
                if (bounds.min.z > point.z) bounds.min.z = point.z;
                if (bounds.max.x < point.x) bounds.max.x = point.x;
                if (bounds.max.y < point.y) bounds.max.y = point.y;
                if (bounds.max.z < point.z) bounds.max.z = point.z;
            }
            
            ReflectionHelper.SetField(ScriptableObject, "_bounds", new RoomScriptableObject.BoundsInfo
            {
                Min = bounds.min,
                Max = bounds.max
            });

            ReflectionHelper.SetField(ScriptableObject, "_size", new int3(
                x: math.abs(bounds.max.x - bounds.min.x),
                y: math.abs(bounds.max.y - bounds.min.y),
                z: math.abs(bounds.max.z - bounds.min.z)
            ));
        }

        private void RefreshTileMatrix()
        {
            var tileMatrix = GetActualTileMatrix();
            
            ReflectionHelper.SetField(ScriptableObject, "_tileMatrix", tileMatrix);
        }

        private List<RoomScriptableObject.TileDescription> GetActualTileMatrix()
        {
            var rootGameObject = _builderUtility.gameObject;
            var basicView      = rootGameObject.transform.Find("Basic View");

            var basicViewChildren = basicView.transform.GetChildren(1);
            var tag               = UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Tile);

            var grid = Grid.Create(
                new float3(1, 1, 1), 
                Grid.GridMode.Local, 
                basicView.position,
                basicView.rotation.ToMathematics());
            var list = new List<RoomScriptableObject.TileDescription>();
            
            foreach (var basicViewChild in basicViewChildren)
            {
                if (basicViewChild.CompareTag(tag))
                {
                    var position  = basicViewChild.transform.position;
                    var rotation  = basicViewChild.transform.rotation; 
                    var gridIndex = grid.ToGrid(position);
                    var identity  = FindTileIdentity(basicViewChild.gameObject);
                    
                    if (list.Any(x => x.Point.Equals(gridIndex))) continue;
                    
                    list.Add(new RoomScriptableObject.TileDescription
                    {
                        Identity = identity,
                        Point    = gridIndex,
                        Rotation = rotation.ToMathematics()
                    });
                }
            }
            
            return list;
        }

        private string FindTileIdentity(GameObject gameObject)
        {
            var label = gameObject.GetComponent<Label>();
            var editorInstance  = WorldDesignerProjectPreferences.EditorInstance;

            var result = editorInstance.Tiles.FirstOrDefault(x => label.HasLabel(x.Identity));
            
            if (result == null) throw new Exception($"Could not find tile of {gameObject}");
            
            return result.Identity;
        }

        private void TakeSnapshot()
        {
            var savedTileMatrix = ScriptableObject.TileMatrix.Select(x => x.Value).ToList();
            var tileMatrix = GetActualTileMatrix();

            var difference = GetTileMatrixDifference(tileMatrix, savedTileMatrix);

            var addOperations = difference.Added.Select(x => new RoomScriptableObject.LocalSnapshot.TileOperation
            {
                Type = RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Add,
                Tile = x
            });

            var removeOperations = difference.Removed.Select(x => new RoomScriptableObject.LocalSnapshot.TileOperation
            {
                Type = RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Remove,
                Tile = x
            });
            
            var operations = addOperations.Concat(removeOperations).ToList();

            var snapshot = new RoomScriptableObject.LocalSnapshot
            {
                Identity   = _takeSnapshotIdentityTextField.value,
                Operations = operations,
            };
            
            var snapshots = ScriptableObject.Snapshots.ToList();
            snapshots.Add(snapshot);
            
            ReflectionHelper.SetField(ScriptableObject, "_snapshots", snapshots);
        }

        private (List<RoomScriptableObject.TileDescription> Added,
            List<RoomScriptableObject.TileDescription> Removed) GetTileMatrixDifference(
                List<RoomScriptableObject.TileDescription> newList,
                List<RoomScriptableObject.TileDescription> oldList)
        {
            var oldMap = oldList.ToDictionary(t => t.Point);
            var newMap = newList.ToDictionary(t => t.Point);

            var added = newList
                .Where(n => !oldMap.TryGetValue(n.Point, out var o)
                            || o.Identity != n.Identity
                            || !o.Rotation.Equals(n.Rotation))
                .ToList();

            var removed = oldList
                .Where(o => !newMap.TryGetValue(o.Point, out var n)
                            || o.Identity != n.Identity
                            || !o.Rotation.Equals(n.Rotation))
                .ToList();

            return (added, removed);
        }

        private void RemoveSnapshot(int value)
        {
            var snapshots = ScriptableObject.Snapshots.ToList();
            snapshots.RemoveAt(value);
            
            ReflectionHelper.SetField(ScriptableObject, "_snapshots", snapshots);
        }

        #endregion

        #region Construction

        private void ReconstructTileMatrix()
        {
            var previous = _editMode;
            _editMode = EditMode.Freeze;
            
            var set            = WorldDesignerProjectPreferences.EditorInstance;
            var tileMatrix     = ScriptableObject.TileMatrix;
            var enterGates     = ScriptableObject.EnterGates;
            var exitGates     = ScriptableObject.ExitGates;
            var rootGameObject = _builderUtility.gameObject;
            var basicView      = rootGameObject.transform.Find("Basic View");
            
            basicView.transform.Clear();
            
            var grid = Grid.Create(
                new float3(1, 1, 1), 
                Grid.GridMode.Local, 
                basicView.position,
                basicView.rotation.ToMathematics());

            foreach (var tile in tileMatrix)
            {
                var gridIndex            = tile.Value.Point;
                var tileIdentity         = tile.Value.Identity;
                var tileScriptableObject = set.Tiles.FirstOrDefault(x => x.Identity.Equals(tileIdentity));
                
                if (tileScriptableObject == null) continue;
                
                var worldPoint = grid.ToWorld(gridIndex);
                var rotation   = tile.Value.Rotation;
                var tilePrefab = tileScriptableObject.Prefab;

                var operation = GameObject.InstantiateAsync(tilePrefab, 1, (Vector3)worldPoint, rotation.ToUnity(), new InstantiateParameters
                {
                    parent            = basicView.transform,
                    scene             = EditorSceneManager.GetActiveScene(),
                    worldSpace        = false,
                    originalImmutable = true
                });

                operation.completed += (result) =>
                {
                    if (exitGates.Any(x => x.Point.Equals(gridIndex)))
                    {
                        var firstExitEntry = exitGates.First(x => x.Point.Equals(gridIndex));
                        var roomExit   = operation.Result[0].gameObject.AddComponent<Exit>();
                        
                        ReflectionHelper.SetField(roomExit, "_snapshotIdentity", firstExitEntry.SnapshotIdentity);
                        ReflectionHelper.SetField(roomExit, "_direction", firstExitEntry.Direction);
                        ReflectionHelper.SetField(roomExit, "_start", firstExitEntry.Pivot);
                    }
                    else if (enterGates.Any(x => x.Point.Equals(gridIndex)))
                    {
                        var firstEnterEntry = exitGates.First(x => x.Point.Equals(gridIndex));
                        var roomEnter       = operation.Result[0].gameObject.AddComponent<Enter>();
                        
                        ReflectionHelper.SetField(roomEnter, "_snapshotIdentity", firstEnterEntry.SnapshotIdentity);
                        ReflectionHelper.SetField(roomEnter, "_direction",        firstEnterEntry.Direction);
                        ReflectionHelper.SetField(roomEnter, "_end",              firstEnterEntry.Pivot);
                    }
                };
            }
            
            _editMode = previous;
        }

        private void ReconstructSnapshot(RoomScriptableObject.LocalSnapshot snapshot)
        {
            var operations     = snapshot.Operations;
            var set            = WorldDesignerProjectPreferences.EditorInstance;
            var rootGameObject = _builderUtility.gameObject;
            var basicView      = rootGameObject.transform.Find("Basic View");
            var basicViewChildren = basicView.transform.GetChildren(1);
            
            var grid = Grid.Create(
                new float3(1, 1, 1), 
                Grid.GridMode.Local, 
                basicView.position,
                basicView.rotation.ToMathematics());
            var tag               = UnityGameObjectTagFabric.Get(UnityGameObjectTagEnum.Tile);

            foreach (var operation in operations)
            {
                if (operation.Type is RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Add)
                {
                    var gridIndex            = operation.Tile.Point;
                    var tileIdentity         = operation.Tile.Identity;
                    var tileScriptableObject = set.Tiles.FirstOrDefault(x => x.Identity.Equals(tileIdentity));
                
                    if (tileScriptableObject == null) continue;
                    
                    var worldPoint = grid.ToWorld(gridIndex);
                    var rotation   = operation.Tile.Rotation;
                    var tilePrefab = tileScriptableObject.Prefab;
                
                    GameObject.InstantiateAsync(tilePrefab, 1, (Vector3)worldPoint, rotation.ToUnity(), new InstantiateParameters
                    {
                        parent            = basicView.transform,
                        scene             = EditorSceneManager.GetActiveScene(),
                        worldSpace        = false,
                        originalImmutable = true
                    });
                } else if (operation.Type is RoomScriptableObject.LocalSnapshot.TileOperation.OperationType.Remove)
                {
                    foreach (var basicViewChild in basicViewChildren)
                    {
                        if (basicViewChild == null) continue;
                        if (basicViewChild.CompareTag(tag))
                        {
                            var position  = basicViewChild.transform.position;
                            var rotation  = basicViewChild.transform.rotation; 
                            var gridIndex = grid.ToGrid(position);
                            var identity  = FindTileIdentity(basicViewChild.gameObject);
                    
                            if (gridIndex.Equals(operation.Tile.Point) && identity.Equals(operation.Tile.Identity))
                                GameObject.DestroyImmediate(basicViewChild.gameObject);
                        }
                    }
                }
            }
        }

        #endregion
    }
}