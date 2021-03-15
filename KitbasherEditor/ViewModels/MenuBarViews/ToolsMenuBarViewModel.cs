using Common;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using View3D.Services;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolsMenuBarViewModel : NotifyPropertyChangedImpl
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;

        public ICommand DivideSubMeshCommand { get; set; }
        public ICommand MergeObjectCommand { get; set; }
        public ICommand DuplicateObjectCommand { get; set; }
        public ICommand DeleteObjectCommand { get; set; }
        public ICommand MergeVertexCommand { get; set; }
        public ICommand CreateLodCommand { get; set; }
        public ICommand ExpandSelectedFacesToObjectCommand { get; set; }

        bool _showObjectTools = true;
        public bool ShowObjectTools { get => _showObjectTools; set => SetAndNotify(ref _showObjectTools, value); }


        bool _showFaceTools = false;
        public bool ShowFaceTools { get => _showFaceTools; set => SetAndNotify(ref _showFaceTools, value); }


        bool _showVertexTools = false;
        public bool ShowVertexTools { get => _showVertexTools; set => SetAndNotify(ref _showVertexTools, value); }


        bool _divideSubMeshEnabled;
        public bool DivideSubMeshEnabled { get => _divideSubMeshEnabled; set => SetAndNotify(ref _divideSubMeshEnabled, value); }

        bool _mergeMeshEnabled;
        public bool MergeMeshEnabled { get => _mergeMeshEnabled; set => SetAndNotify(ref _mergeMeshEnabled, value); }

        bool _duplicateEnabled;
        public bool DuplicateEnabled { get => _duplicateEnabled; set => SetAndNotify(ref _duplicateEnabled, value); }

        bool _deleteEnabled;
        public bool DeleteEnabled { get => _deleteEnabled; set => SetAndNotify(ref _deleteEnabled, value); }

        bool _mergeVertexEnabled;
        public bool MergeVertexEnabled { get => _mergeVertexEnabled; set => SetAndNotify(ref _mergeVertexEnabled, value); }

        bool _expandSelectedFacesToObjectEnabled;
        public bool ExpandSelectedFacesToObjectEnabled { get => _expandSelectedFacesToObjectEnabled; set => SetAndNotify(ref _expandSelectedFacesToObjectEnabled, value); }


        public ToolsMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            DivideSubMeshCommand = new RelayCommand(DivideSubMesh);
            MergeObjectCommand = commandFactory.Register(new RelayCommand(MergeObjects), Key.M, ModifierKeys.Control);
            DuplicateObjectCommand = commandFactory.Register(new RelayCommand(DubplicateObject), Key.D, ModifierKeys.Control);
            DeleteObjectCommand = commandFactory.Register(new RelayCommand(DeleteObject), Key.Delete, ModifierKeys.None);
            MergeVertexCommand = new RelayCommand(MergeVertex);
            CreateLodCommand = new RelayCommand(CreateLod);
            ExpandSelectedFacesToObjectCommand = new RelayCommand(ExpandFaceSelection);
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;

            _objectEditor = componentManager.GetComponent<ObjectEditor>();
            _faceEditor = componentManager.GetComponent<FaceEditor>();
            OnSelectionChanged(_selectionManager.GetState());
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            ShowObjectTools = state is ObjectSelectionState;
            ShowFaceTools = state is FaceSelectionState;
            ShowVertexTools = state is VertexSelectionState;

            DivideSubMeshEnabled = false;
            DuplicateEnabled = false;
            DeleteEnabled = false;
            MergeMeshEnabled = false;
            ExpandSelectedFacesToObjectEnabled = false;
            MergeVertexEnabled = false;

            if (state is ObjectSelectionState objectSelection)
            {
                DivideSubMeshEnabled = objectSelection.SelectedObjects().Count == 1;
                MergeMeshEnabled = objectSelection.SelectedObjects().Count >= 2;
                DuplicateEnabled = objectSelection.SelectedObjects().Count > 0;
                DeleteEnabled = objectSelection.SelectedObjects().Count > 0;
            }
            else if (state is FaceSelectionState faceSelection && faceSelection.SelectedFaces.Count != 0)
            {
                DeleteEnabled = true;
                ExpandSelectedFacesToObjectEnabled = true;
                //DuplicateEnabled = true;
                //DivideSubMeshEnabled = true;
            }
            else
            {
                // Vertex state
            }
        }

        void DivideSubMesh() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DivideIntoSubmeshes(objectSelectionState);
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.ConvertSelectionToSeperateMesh(faceSelectionState, true);
        }

        void MergeObjects() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
            {
                if (objectSelectionState.CurrentSelection().Count >= 2)
                {
                    if (!_objectEditor.CombineMeshes(objectSelectionState, out var errorMessage))
                    {
                        var longError = string.Join("\n", errorMessage);
                        MessageBox.Show("Errors trying to combine meshes:\n\n" + longError);
                    }
                }                
            }
        }

        void DubplicateObject()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DuplicateObject(objectSelectionState);
            if (_selectionManager.GetState() is FaceSelectionState faceSelectionState)
                _faceEditor.ConvertSelectionToSeperateMesh(faceSelectionState, false);
        }
        
        void DeleteObject() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DeleteObject(objectSelectionState);
            else if (_selectionManager.GetState() is FaceSelectionState faceSelection)
                _faceEditor.DeleteFaces(faceSelection);
        }

        void MergeVertex()
        {
        }

        void ExpandFaceSelection()
        {
            _faceEditor.GrowSelection(_selectionManager.GetState() as FaceSelectionState);
        }

        void CreateLod()
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState && objectSelectionState.SelectionCount() != 0)
                _objectEditor.ReduceSelection(objectSelectionState.SelectedObjects(), 0.5f);
        }


    }
}
