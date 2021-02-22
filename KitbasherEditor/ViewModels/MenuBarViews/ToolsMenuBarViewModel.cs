using Common;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;
using View3D.Components.Component;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolsMenuBarViewModel : NotifyPropertyChangedImpl
    {
        SelectionManager _selectionManager;
        ObjectEditor _objectEditor;
        FaceEditor _faceEditor;

        public ICommand DivideSubMeshCommand { get; set; }
        public ICommand MergeObjectCommand { get; set; }
        public ICommand FreezeTransformCommand { get; set; }
        public ICommand DuplicateObjectCommand { get; set; }
        public ICommand DeleteObjectCommand { get; set; }
        public ICommand MergeVertexCommand { get; set; }


        bool _divideSubMeshEnabled;
        public bool DivideSubMeshEnabled { get => _divideSubMeshEnabled; set => SetAndNotify(ref _divideSubMeshEnabled, value); }

        bool _mergeMeshEnabled;
        public bool MergeMeshEnabled { get => _mergeMeshEnabled; set => SetAndNotify(ref _mergeMeshEnabled, value); }

        bool _freezeTransformEnabled;
        public bool FreezeTransformEnabled { get => _freezeTransformEnabled; set => SetAndNotify(ref _freezeTransformEnabled, value); }

        bool _duplicateEnabled;
        public bool DuplicateEnabled { get => _duplicateEnabled; set => SetAndNotify(ref _duplicateEnabled, value); }

        bool _deleteEnabled;
        public bool DeleteEnabled { get => _deleteEnabled; set => SetAndNotify(ref _deleteEnabled, value); }

        bool _mergeVertexEnabled;
        public bool MergeVertexEnabled { get => _mergeVertexEnabled; set => SetAndNotify(ref _mergeVertexEnabled, value); }


        public ToolsMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            DivideSubMeshCommand = new RelayCommand(DivideSubMesh);
            MergeObjectCommand = commandFactory.Register(new RelayCommand(MergeObjects), Key.M, ModifierKeys.Control);
            FreezeTransformCommand = new RelayCommand(FreezeObject);
            DuplicateObjectCommand = commandFactory.Register(new RelayCommand(DubplicateObject), Key.D, ModifierKeys.Control);
            DeleteObjectCommand = commandFactory.Register(new RelayCommand(DeleteObject), Key.Delete, ModifierKeys.None);
            MergeVertexCommand = new RelayCommand(MergeVertex);

            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _selectionManager.SelectionChanged += OnSelectionChanged;

            _objectEditor = componentManager.GetComponent<ObjectEditor>();
            _faceEditor = componentManager.GetComponent<FaceEditor>();
            OnSelectionChanged(_selectionManager.GetState());
        }

        private void OnSelectionChanged(ISelectionState state)
        {
            if (state is ObjectSelectionState objectSelection)
            {
                if (objectSelection.SelectedObjects().Count == 1)
                {
                    DivideSubMeshEnabled = true;
                    DuplicateEnabled = true;
                    DeleteEnabled = true;
                }
                else if (objectSelection.SelectedObjects().Count > 0)
                {
                    DivideSubMeshEnabled = false;
                    DuplicateEnabled = true;
                    DeleteEnabled = true;
                }
                else
                {
                    DivideSubMeshEnabled = false;
                    DuplicateEnabled = false;
                    DeleteEnabled = false;
                }
            }
            else if (state is FaceSelectionState faceSelection && faceSelection.SelectedFaces.Count != 0)
            {
                DivideSubMeshEnabled = false;
                DuplicateEnabled = false;
                DeleteEnabled = true;
            }
            else
            {
                DivideSubMeshEnabled = false;
                DuplicateEnabled = false;
                DeleteEnabled = false;
            }

            MergeMeshEnabled = false;
            FreezeTransformEnabled = false;
            MergeVertexEnabled = false;
        }


        void DivideSubMesh() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DivideIntoSubmeshes(objectSelectionState);
        }

        void MergeObjects() { }
        void FreezeObject() { }

        void DubplicateObject() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DuplicateObject(objectSelectionState);
        }
        
        void DeleteObject() 
        {
            if (_selectionManager.GetState() is ObjectSelectionState objectSelectionState)
                _objectEditor.DeleteObject(objectSelectionState);
            else if (_selectionManager.GetState() is FaceSelectionState faceSelection)
                _faceEditor.DeleteFaces(faceSelection);
        }

        void MergeVertex() { }


    }
}
