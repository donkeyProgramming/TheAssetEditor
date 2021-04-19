using Common;
using CommonControls.PackFileBrowser;
using GalaSoft.MvvmLight.CommandWpf;
using KitbasherEditor.Services;
using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
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
        public ICommand GenerateWsModelCommand { get; set; }
        public ICommand OpenRefereceFileCommand { get; set; }
        public ICommand ValidatCommand { get; set; }
        public ICommand UndoCommand { get; set; }
        public ICommand DeleteHistoryCommand { get; set; }

        public ICommand FocusCameraCommand { get; set; }
        public ICommand ResetCameraCommand { get; set; }
        public ICommand SortModelsByNameCommand { get; set; }
        

        string _undoHintText;
        public string UndoHintText { get => _undoHintText; set => SetAndNotify(ref _undoHintText, value); }

        bool _undoEnabled;
        public bool UndoEnabled { get => _undoEnabled; set => SetAndNotify(ref _undoEnabled, value); }

        CommandExecutor _commandExecutor;
        FocusSelectableObjectComponent _cameraFocusComponent;
        SelectionManager _selectionManager;
        IEditableMeshResolver _editableMeshResolver;
        ObjectEditor _objectEditor;
        public ModelSaverHelper ModelSaver { get; set; }

        public GeneralMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            SaveCommand = commandFactory.Register(new RelayCommand(() => ModelSaver.Save()), Key.S, ModifierKeys.Control);
            SaveAsCommand = new RelayCommand(() => ModelSaver.SaveAs());
            GenerateWsModelCommand = new RelayCommand(() => ModelSaver.GenerateWsModel());
            OpenRefereceFileCommand = commandFactory.Register(new RelayCommand(OpenReferenceFile), Key.O, ModifierKeys.Control);
            ValidatCommand = new RelayCommand(Validate);

            UndoCommand = commandFactory.Register(new RelayCommand(() => _commandExecutor.Undo()), Key.Z, ModifierKeys.Control);
            DeleteHistoryCommand = new RelayCommand(DeleteHistory);

            FocusCameraCommand = commandFactory.Register(new RelayCommand(() => _cameraFocusComponent.FocusSelection()), Key.F, ModifierKeys.Control);
            ResetCameraCommand = new RelayCommand(() => _cameraFocusComponent.ResetCamera());

            SortModelsByNameCommand = new RelayCommand(SortMeshes);

            _commandExecutor = componentManager.GetComponent<CommandExecutor>();
            _commandExecutor.CommandStackChanged += OnUndoStackChanged;

            _cameraFocusComponent = componentManager.GetComponent<FocusSelectableObjectComponent>();
            _selectionManager = componentManager.GetComponent<SelectionManager>();
            _editableMeshResolver = componentManager.GetComponent<IEditableMeshResolver>();
            _objectEditor = componentManager.GetComponent<ObjectEditor>();
        }

        private void OnUndoStackChanged()
        {
            UndoHintText = _commandExecutor.GetUndoHint();
            UndoEnabled = _commandExecutor.CanUndo();
        }

        void OpenReferenceFile() { }
        
        void Validate() { }
        
        void DeleteHistory() { }

        void SortMeshes()
        {
            var lod0 = _editableMeshResolver.GeEditableMeshRootNode().GetLodNodes().FirstOrDefault();
            if (lod0 != null)
                _objectEditor.SortMeshes(lod0);
        }
    }
}
