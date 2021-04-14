using Common;
using CommonControls.Common;
using CommonControls.PackFileBrowser;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using KitbasherEditor.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelSaverHelper
    {
        ILogger _logger = Logging.Create<ModelSaverHelper>();

        private readonly  PackFileService _packFileService;
        private readonly  SceneManager _sceneManager;
        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly MainEditableNode _editableMeshNode;

        public ModelSaverHelper(PackFileService packFileService, SceneManager sceneManager, KitbasherViewModel kitbasherViewModel, MainEditableNode editableMeshNode)
        {
           _packFileService = packFileService;
           _sceneManager = sceneManager;
           _kitbasherViewModel = kitbasherViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void Save()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();
                var path = _packFileService.GetFullPath(inputFile);
                var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                if (res != null)
                    _kitbasherViewModel.MainFile = res;
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        public void SaveAs()
        {
            try
            {
                var inputFile = _kitbasherViewModel.MainFile as PackFile;
                byte[] bytes = GetBytesToSave();

                using (var browser = new SavePackFileWindow(_packFileService))
                {
                    browser.ViewModel.Filter.SetExtentions(new List<string>() {".rigid_model_v2" });
                    if (browser.ShowDialog() == true)
                    {
                        var path = browser.FilePath;
                        if (path.Contains(".rigid_model_v2") == false)
                            path += ".rigid_model_v2";

                        var res = SaveHelper.Save(_packFileService, path, inputFile, bytes);
                        if (res != null)
                            _kitbasherViewModel.MainFile = res;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Here().Error("Error saving model - " + e.ToString());
                MessageBox.Show("Saving failed!");
            }
        }

        private byte[] GetBytesToSave()
        {
            var isAllVisible = true;
            _editableMeshNode.GetLodNodes()[0].ForeachNode((node) =>
            {
                if (!node.IsVisible)
                    isAllVisible = false;
            });

            bool onlySaveVisible = false;
            if (isAllVisible == false)
            {
                if (MessageBox.Show("Only save visible nodes?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    onlySaveVisible = true;
            }

            List<string> boneNames = new List<string>();
            if(_kitbasherViewModel.Animation.Skeleton != null)
                boneNames = _kitbasherViewModel.Animation.Skeleton.BoneNames.ToList();
            var bytes = _editableMeshNode.Save(onlySaveVisible, boneNames);
            var reloadedModel = new RmvRigidModel(bytes, "reloadedFile");
            return bytes;
        }
    }
}
