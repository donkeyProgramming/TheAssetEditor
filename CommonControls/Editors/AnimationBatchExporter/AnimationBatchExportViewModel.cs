// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommonControls.BaseDialogs;
using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Common;
using CommonControls.Editors.AnimationFilePreviewEditor;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Serilog;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Editors.AnimationBatchExporter
{
    public class AnimationBatchExportViewModel
    {
        ILogger _logger = Logging.Create<AnimationBatchExportViewModel>();
        PackFileService _pfs;

        public ObservableCollection<PackFileListItem> PackfileList { get; set; } = new ObservableCollection<PackFileListItem>();
        public ObservableCollection<uint> PossibleOutputFormats { get; set; } = new ObservableCollection<uint>() { 5, 6, 7 };
        public NotifyAttr<uint> SelectedOutputFormat { get; set; } = new NotifyAttr<uint>(7);

        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public AnimationBatchExportViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;

            var containers = _pfs.GetAllPackfileContainers();
            foreach (var item in containers)
            {
                if (item == _pfs.GetEditablePack())
                    continue;
                PackfileList.Add(new PackFileListItem(item));
            }
        }

        public void Process()
        {
            var outputPack = _pfs.GetEditablePack();
            if (outputPack == null)
            {
                MessageBox.Show("No output packfile selectd. Please set Editable pack before running the converter", "Error");
                return;
            }

            if (MessageBox.Show("The converter will preplace any file with overlapping names in the output folder. Are you sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            var errorList = new ErrorList();

            using (new WaitCursor())
            {
                foreach (var packfile in PackfileList)
                {
                    if (packfile.Process.Value == false)
                        continue;

                    _logger.Here().Information($"Processing packfile container {packfile.Name}");

                    var animFiles = _pfs.FindAllWithExtention(".anim", packfile.Container);

                    _logger.Here().Information($"Converting animations {animFiles.Count}");
                    var convertedAnimFiles = ConvertAnimFiles(animFiles, SelectedOutputFormat.Value, errorList);

                    _logger.Here().Information($"saving animation files");
                    _pfs.AddFilesToPack(_pfs.GetEditablePack(),
                        convertedAnimFiles.Select(x => x.directory).ToList(),
                        convertedAnimFiles.Select(x => x.file).ToList());

                    _logger.Here().Information($"Saving inv matix files");
                    var invMatrixFileList = _pfs.FindAllWithExtention(".bone_inv_trans_mats", packfile.Container);
                    foreach (var invMatrixFile in invMatrixFileList)
                        _pfs.CopyFileFromOtherPackFile(packfile.Container, _pfs.GetFullPath(invMatrixFile), _pfs.GetEditablePack());
                }
            }

            ErrorListWindow.ShowDialog("Bach result", errorList, true);
        }

        List<(PackFile file, string directory)> ConvertAnimFiles(List<PackFile> packFiles, uint outputAnimationFormat, ErrorList errorList)
        {
            var output = new List<(PackFile file, string directory)>();

            foreach (var file in packFiles)
            {
                try
                {
                    var animationFile = AnimationFile.Create(file);
                    animationFile.ConvertToVersion(outputAnimationFormat, _skeletonAnimationLookUpHelper, _pfs);

                    var bytes = AnimationFile.ConvertToBytes(animationFile);
                    var newPackFile = new PackFile(file.Name, new MemorySource(bytes));

                    var path = _pfs.GetFullPath(file);
                    var directoryPath = Path.GetDirectoryName(path);

                    output.Add((newPackFile, directoryPath));
                }
                catch (Exception e)
                {
                    var path = _pfs.GetFullPath(file);
                    errorList.Error(path, e.Message);
                }
            }

            return output;
        }



        public class PackFileListItem
        {
            public PackFileContainer Container { get; private set; }

            public PackFileListItem(PackFileContainer item)
            {
                Name.Value = item.Name;
                Container = item;
            }

            public NotifyAttr<bool> Process { get; set; } = new NotifyAttr<bool>(true);
            public NotifyAttr<string> Name { get; set; } = new NotifyAttr<string>("");
        }

    }
}
