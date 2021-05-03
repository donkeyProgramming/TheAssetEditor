using AnimationEditor.Common.AnimationSelector;
using Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace AnimationEditor.Common.MeshSelector
{
    /*public class MeshSelectorViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pf;
        AnimationSelectorViewModel _animationSelectorViewModel;

        public ICommand BrowseFileCommand { get; set; }


        string _selectedFileName = "None";
        public string SelectedFileName { get => _selectedFileName; set => SetAndNotify(ref _selectedFileName, value); }

        public MeshSelectorViewModel(PackFileService pf, AnimationSelectorViewModel animationSelectorViewModel)
        {
            _pf = pf;
            _animationSelectorViewModel = animationSelectorViewModel;
            BrowseFileCommand = new RelayCommand(BrowseFile);
        }

        void BrowseMesh()
        { 
        
        }

        void BrowseFile()
        {
            using (var browser = new PackFileBrowserWindow(_pf))
            {
                browser.ViewModel.Filter.SetExtentions(new List<string>() { ".variantmeshdefinition", ".wsmodel", ".rigid_model_v2" });
                if (browser.ShowDialog() == true && browser.SelectedFile != null)
                {
                    var file = browser.SelectedFile;
                    var rmv = new RmvRigidModel(file.DataSource.ReadData(), file.Name);

                    //EditableMeshNode.SetModel(rmv, _resourceLibary, _animPlayer, GeometryGraphicsContextFactory.CreateInstance(_resourceLibary.GraphicsDevice));
                    //ModelLoader.LoadReference(browser.SelectedFile);
                }
            }
        }
    }*/
}
