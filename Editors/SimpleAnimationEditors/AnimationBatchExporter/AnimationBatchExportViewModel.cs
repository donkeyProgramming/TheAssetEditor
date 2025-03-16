using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommonControls.BaseDialogs.ErrorListDialog;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.ToolCreation;
using Shared.GameFormats.Animation;
using Shared.Ui.Common;

namespace CommonControls.Editors.AnimationBatchExporter
{
    public class AnimationBatchExportViewModel : IEditorInterface
    {
        private readonly ILogger _logger = Logging.Create<AnimationBatchExportViewModel>();
        private readonly IPackFileService _pfs;
        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        public ObservableCollection<PackFileListItem> PackfileList { get; set; } = [];
        public ObservableCollection<uint> PossibleOutputFormats { get; set; } = [5, 6, 7];
        public NotifyAttr<uint> SelectedOutputFormat { get; set; } = new NotifyAttr<uint>(7);


        public string DisplayName { get; set; } = "Animation Batch Exporter";

     

        public AnimationBatchExportViewModel(IPackFileService pfs, ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
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

                    var animFiles = PackFileServiceUtility.FindAllWithExtention(_pfs, ".anim", packfile.Container);

                    _logger.Here().Information($"Converting animations {animFiles.Count}");
                    var convertedAnimFiles = ConvertAnimFiles(animFiles, SelectedOutputFormat.Value, errorList);

                    _logger.Here().Information($"saving animation files");

                    var filesToAdd = convertedAnimFiles.Select(x => new NewPackFileEntry(x.directory, x.file)).ToList();

                    _pfs.AddFilesToPack(_pfs.GetEditablePack(), filesToAdd);

                    _logger.Here().Information($"Saving inv matix files");
                    var invMatrixFileList = PackFileServiceUtility.FindAllWithExtention(_pfs, ".bone_inv_trans_mats", packfile.Container);
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
                    var skeleton = _skeletonAnimationLookUpHelper.GetSkeletonFileFromName(animationFile.Header.SkeletonName);
                    animationFile.ConvertToVersion(outputAnimationFormat, skeleton, _pfs);

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

        public void Close()
        {
     
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
