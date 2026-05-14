using System;
using System.ComponentModel;
using System.Windows;
using Serilog;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu;

namespace Shared.Ui.BaseDialogs.StandardDialog.PackFile
{
    public partial class SavePackFileWindow : Window, IDisposable, INotifyPropertyChanged
    {
        private static readonly ILogger s_logger = Logging.Create<SavePackFileWindow>();
        readonly IPackFileService _packfileService;

        public Core.PackFiles.Models.PackFile SelectedFile { get; set; }
        public PackFileBrowserViewModel ViewModel { get; set; }

        TreeNode _selectedNode;
        string _currentFileName;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentFileName { get => _currentFileName; set { _currentFileName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFileName")); SelectedFile = null; } }


        public string FilePath { get; private set; }

        public SavePackFileWindow(IPackFileService packfileService, PackFileTreeViewFactory packFileBrowserBuilder)
        {
            _packfileService = packfileService;
            ViewModel = packFileBrowserBuilder.Create(ContextMenuType.Simple, showCaFiles: false, showFoldersOnly: false);
            ViewModel.FileOpen += ViewModel_FileOpen;
            ViewModel.NodeSelected += ViewModel_FileSelected;
            InitializeComponent();
            DataContext = this;
        }

        private void ViewModel_FileSelected(TreeNode node)
        {
            _selectedNode = node;

            if (_selectedNode.Item == null)
                CurrentFileName = "";
            else
                CurrentFileName = _selectedNode.Item.Name;

            SelectedFile = _selectedNode.Item;
        }

        private void ViewModel_FileOpen(Core.PackFiles.Models.PackFile file)
        {
            SelectedFile = file;
            Button_Click(null, null);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentFileName))
            {
                s_logger.Here().Warning("Save pack file dialog was confirmed without a file name");
                MessageBox.Show("No name provided, can not save file");
                return;
            }

            if (SelectedFile != null)
            {
                if (MessageBox.Show("Replace file?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    s_logger.Here().Information($"Save pack file dialog replace declined for '{SelectedFile.Name}'");
                    return;
                }
            }

            FilePath = BuildTargetPath(_selectedNode, SelectedFile, CurrentFileName, _packfileService).ToLower();
            s_logger.Here().Information($"Save pack file dialog accepted path '{FilePath}'");
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            ViewModel.FileOpen -= ViewModel_FileOpen;
            ViewModel.NodeSelected -= ViewModel_FileSelected;
            ViewModel.Dispose();
            ViewModel = null;
            DataContext = null;
        }

        private static string BuildTargetPath(TreeNode? selectedNode, Core.PackFiles.Models.PackFile? selectedFile, string currentFileName, IPackFileService packfileService)
        {
            if (selectedFile != null)
                return packfileService.GetFullPath(selectedFile);

            var directoryPath = GetSelectedDirectoryPath(selectedNode);
            var normalizedFileName = currentFileName.Replace('/', '\\').Trim().TrimStart('\\');
            return string.IsNullOrWhiteSpace(directoryPath)
                ? normalizedFileName
                : directoryPath + "\\" + normalizedFileName;
        }

        private static string GetSelectedDirectoryPath(TreeNode? selectedNode)
        {
            if (selectedNode == null || selectedNode.NodeType == NodeType.Root)
                return string.Empty;

            if (selectedNode.NodeType == NodeType.File)
                return PathNormalization.NormalizeDirectoryPath(System.IO.Path.GetDirectoryName(selectedNode.GetFullPath()));

            return PathNormalization.NormalizeDirectoryPath(selectedNode.GetFullPath());
        }
    }
}
