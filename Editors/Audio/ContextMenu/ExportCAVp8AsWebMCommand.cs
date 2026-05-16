using System;
using System.IO;
using Editors.Audio.Shared.GameInformation.Warhammer3;
using Editors.Audio.Shared.Storage;
using Editors.Audio.Shared.Utilities;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Ui.BaseDialogs.PackFileTree;
using Shared.Ui.BaseDialogs.PackFileTree.ContextMenu.Commands;

namespace Editors.Audio.ContextMenu
{
    public class ExportCAVp8AsWebMCommand(
        IStandardDialogs standardDialogs,
        IFileSystemAccess fileSystemAccess,
        IPackFileService packFileService,
        IAudioRepository audioRepository,
        IMovieAudioResolver movieAudioResolver) : IContextMenuCommand
    {
        private readonly IStandardDialogs _standardDialogs = standardDialogs;
        private readonly IFileSystemAccess _fileSystemAccess = fileSystemAccess;
        private readonly IPackFileService _packFileService = packFileService;
        private readonly IAudioRepository _audioRepository = audioRepository;
        private readonly IMovieAudioResolver _movieAudioResolver = movieAudioResolver;

        public string GetDisplayName(TreeNode node) => "Export as WebM";
        public bool ShouldAdd(TreeNode node) => node.NodeType == NodeType.File && node.Item != null;
        public bool IsEnabled(TreeNode node) => node.Item != null && node.Item.Name.EndsWith(".ca_vp8", StringComparison.OrdinalIgnoreCase);

        public void Execute(TreeNode selectedNode)
        {
            var caVp8PackFile = selectedNode.Item;
            if (caVp8PackFile == null)
                return;

            var dialogResult = _standardDialogs.ShowSystemFolderBrowserDialog();
            if (!dialogResult.Result || string.IsNullOrWhiteSpace(dialogResult.FolderPath))
                return;

            DirectoryHelper.EnsureCreated(dialogResult.FolderPath);

            _audioRepository.Load(Wh3LanguageInformation.GetAllLanguages());

            var caVp8PackFilePath = _packFileService.GetFullPath(caVp8PackFile);
            var wemPackFile = _movieAudioResolver.ResolveMovieWem(caVp8PackFilePath);
            var webMPath = Path.Combine(dialogResult.FolderPath, Path.ChangeExtension(caVp8PackFile.Name, ".webm"));
            var webMBytes = CAVp8Exporter.ExportToWebM(caVp8PackFile, wemPackFile);
            _fileSystemAccess.FileWriteAllBytes(webMPath, webMBytes);
        }
    }
}
