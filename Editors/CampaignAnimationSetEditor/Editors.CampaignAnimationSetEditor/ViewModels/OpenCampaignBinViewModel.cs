using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.CampaignAnimationSetEditor.ViewModels
{
    public class OpenCampaignBinEntry(string path, PackFile file)
    {
        public string Path { get; } = path;
        public PackFile File { get; } = file;
    }

    /// <summary>Lists the .bin files under the given folder across all loaded packs, for the
    /// toolbar's Open button. Scoped to that folder because it's the only place this editor's
    /// format is valid; the app's generic browse dialog has no folder scoping.</summary>
    public partial class OpenCampaignBinViewModel : ObservableObject
    {
        public ObservableCollection<OpenCampaignBinEntry> Files { get; } = [];

        [ObservableProperty] OpenCampaignBinEntry? _selectedFile;
        [ObservableProperty] string _filterText = "";

        readonly List<OpenCampaignBinEntry> _allFiles;

        public OpenCampaignBinViewModel(IPackFileService packFileService, string folder)
        {
            _allFiles = packFileService.FindAllWithExtention(".bin")
                .Where(x => x.FileName.Replace('/', '\\').Contains(folder, StringComparison.OrdinalIgnoreCase))
                .Select(x => new OpenCampaignBinEntry(x.FileName, x.Pack))
                .OrderBy(x => x.Path)
                .ToList();

            foreach (var file in _allFiles)
                Files.Add(file);
        }

        partial void OnFilterTextChanged(string value)
        {
            Files.Clear();
            var matches = string.IsNullOrWhiteSpace(value)
                ? _allFiles
                : _allFiles.Where(x => x.Path.Contains(value, StringComparison.OrdinalIgnoreCase));

            foreach (var file in matches)
                Files.Add(file);
        }
    }
}
