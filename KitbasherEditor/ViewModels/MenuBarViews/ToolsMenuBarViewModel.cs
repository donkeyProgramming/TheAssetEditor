using Common;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using System.Windows.Input;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class ToolsMenuBarViewModel : NotifyPropertyChangedImpl
    {
        public ICommand SplitObjectCommand { get; set; }
        public ICommand MergeObjectCommand { get; set; }
        public ICommand FreezeTransformCommand { get; set; }
        public ICommand DuplicateObjectCommand { get; set; }
        public ICommand DeleteObjectCommand { get; set; }
        public ICommand MergeVertexCommand { get; set; }


        public ToolsMenuBarViewModel(IComponentManager componentManager, ToolbarCommandFactory commandFactory)
        {
            SplitObjectCommand = new RelayCommand(SplitObject);
            MergeObjectCommand = commandFactory.Register(new RelayCommand(MergeObjects), Key.M, ModifierKeys.Control);
            FreezeTransformCommand = new RelayCommand(FreezeObject);
            DuplicateObjectCommand = commandFactory.Register(new RelayCommand(DubplicateObject), Key.D, ModifierKeys.Control);
            DeleteObjectCommand = commandFactory.Register(new RelayCommand(DeleteObject), Key.Delete, ModifierKeys.None);
            MergeVertexCommand = new RelayCommand(MergeVertex);

            //_gizmoComponent = componentManager.GetComponent<GizmoComponent>();
        }

        void SplitObject() { }
        void MergeObjects() { }
        void FreezeObject() { }

        void DubplicateObject() { }
        void DeleteObject() { }
        void MergeVertex() { }


    }
}
