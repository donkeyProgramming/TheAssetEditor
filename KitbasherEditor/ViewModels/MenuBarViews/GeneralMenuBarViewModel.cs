using Common;
using CommonControls.PackFileBrowser;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System.Linq;
using System.Windows.Input;
using View3D.Commands.Object;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;
using static View3D.Commands.Object.GroupObjectsCommand;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class GeneralMenuBarViewModel : NotifyPropertyChangedImpl
    {
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAsCommand { get; set; }
        public ICommand OpenRefereceFileCommand { get; set; }
        public ICommand ValidatCommand { get; set; }
        public ICommand UndoCommand { get; set; }
        public ICommand DeleteHistoryCommand { get; set; }

        public ICommand FocusCameraCommand { get; set; }
        public ICommand ResetCameraCommand { get; set; }

        public ICommand GroupSelectionCommand { get; set; }
        public ICommand UnGroupSelectionCommand { get; set; }

        string _undoHintText;
        public string UndoHintText { get => _undoHintText; set => SetAndNotify(ref _undoHintText, value); }

        bool _undoEnabled;
        public bool UndoEnabled { get => _undoEnabled; set => SetAndNotify(ref _undoEnabled, value); }

        CommandExecutor _commandExecutor;
        FocusSelectableObjectComponent _cameraFocusComponent;
        SelectionManager _selectionManager;
        IEditableMeshResolver _editableMeshResolver;
        public ModelSaverHelper ModelSaver { get; set; }

        public GeneralMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            SaveCommand = commandFactory.Register(new RelayCommand(Save), Key.S, ModifierKeys.Control);
            SaveAsCommand = new RelayCommand(SaveAs);
            OpenRefereceFileCommand = commandFactory.Register(new RelayCommand(OpenReferenceFile), Key.O, ModifierKeys.Control);
            ValidatCommand = new RelayCommand(Validate);

            UndoCommand = commandFactory.Register(new RelayCommand(Undo), Key.Z, ModifierKeys.Control);
            DeleteHistoryCommand = new RelayCommand(DeleteHistory);

            FocusCameraCommand = commandFactory.Register(new RelayCommand(FocusCamera), Key.F, ModifierKeys.Control);
            ResetCameraCommand = new RelayCommand(ResetCamera);

            GroupSelectionCommand = commandFactory.Register(new RelayCommand(Group), Key.G, ModifierKeys.Control);
            UnGroupSelectionCommand = commandFactory.Register(new RelayCommand(UnGroup), Key.G, ModifierKeys.Shift);

            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _commandExecutor.CommandStackChanged += OnUndoStackChanged;

            _cameraFocusComponent = componentManager.GetComponent<FocusSelectableObjectComponent>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
        }

        private void OnUndoStackChanged()
        {
            UndoHintText = _commandExecutor.GetUndoHint();
            UndoEnabled = _commandExecutor.CanUndo();
        }

        void Group()
        {
            var state = _selectionManager.GetStateCopy() as ObjectSelectionState;
            if (state != null && state.SelectedObjects().Count >= 2)
            {
                var cmd = new GroupObjectsCommand(_editableMeshResolver.GetActiveEditableMeshNode(), state.CurrentSelection());
                _commandExecutor.ExecuteCommand(cmd);
            }
        }

        void UnGroup()
        {
            var state = _selectionManager.GetStateCopy() as ObjectSelectionState;
            if (state != null && state.SelectedObjects().Count != 0)
            {
                // Make sure they have the same group
                var parents = state.SelectedObjects().Select(x => x.Parent).Distinct().ToList();
                if (parents.Count == 1 && parents.First() is GroupNode groupNode && groupNode.IsUngroupable)
                {
                     var cmd = new UnGroupObjectsCommand(_editableMeshResolver.GetActiveEditableMeshNode(), state.CurrentSelection(), groupNode);
                     _commandExecutor.ExecuteCommand(cmd);
                }
            }
        }

        void Save() 
        {
            ModelSaver.Save();
        }

        void SaveAs()
        {
            ModelSaver.SaveAs();
        }

        void OpenReferenceFile() 
        { 
        
        }
        
        void Validate() { }
        
        void Undo() 
        {
            _commandExecutor.Undo();
        }

        void DeleteHistory() { }

        void FocusCamera() 
        {
            _cameraFocusComponent.FocusSelection();
        }

        void ResetCamera() 
        {
            _cameraFocusComponent.ResetCamera();
        }
    }
}
