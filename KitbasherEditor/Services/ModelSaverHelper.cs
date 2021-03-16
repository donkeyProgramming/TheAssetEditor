using CommonControls.Common;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using KitbasherEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using View3D.Components.Component;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelSaverHelper
    {
        private readonly  PackFileService _packFileService;
        private readonly  SceneManager _sceneManager;
        private readonly KitbasherViewModel _kitbasherViewModel;
        private readonly Rmv2ModelNode _editableMeshNode;

        public ModelSaverHelper(PackFileService packFileService, SceneManager sceneManager, KitbasherViewModel kitbasherViewModel, Rmv2ModelNode editableMeshNode)
        {
           _packFileService = packFileService;
           _sceneManager = sceneManager;
           _kitbasherViewModel = kitbasherViewModel;
            _editableMeshNode = editableMeshNode;
        }

        public void Save()
        {
            var inputFile = _kitbasherViewModel.MainFile as PackFile;
            var bytes = _editableMeshNode.Save();
            var newPf = new PackFile(inputFile.Name, new MemorySource(bytes));
            var path = _packFileService.GetFullPath(inputFile);
            SaveHelper.Save(_packFileService, path, newPf);

            return;

          
            var selectedEditabelPackFile = _packFileService.GetEditablePack();
            var filePackFileConainer = _packFileService.GetPackFileContainer(inputFile);

            if (selectedEditabelPackFile == null)
            {
                MessageBox.Show("No editable pack selected!");
                return;
            }

            if (filePackFileConainer != selectedEditabelPackFile)
            {
                var filePath = _packFileService.GetFullPath(inputFile, filePackFileConainer);
                _packFileService.CopyFileFromOtherPackFile(filePackFileConainer, filePath, selectedEditabelPackFile);
            }


            
            /*
             RmvRigidModel model = new RmvRigidModel(originalMeshBytes, "UnitTestModel");
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                    model.SaveToByteArray(writer);

                var savedMeshBytes = ms.ToArray();
                Assert.AreEqual(originalMeshBytes.Length, savedMeshBytes.Length);

                for (int i = 0; i < originalMeshBytes.Length; i++)
                    Assert.AreEqual(originalMeshBytes[i], savedMeshBytes[i]);
            }
             
             */

            // _editableMeshNode

        }
    }
}
