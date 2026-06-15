using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Shared.Core.PackFiles.Models;

namespace AssetEditor.ViewModels
{
    public class RecentPackFileItem
    {
        public RecentPackFileItem(string path, PackFileContainerType containerType, bool isReadOnly, Action execute)
        {
            Command = new RelayCommand(execute);
            Path = path;
            Header = $"{System.IO.Path.GetFileName(path)} {BuildKindLabel(containerType, isReadOnly)}";
        }

        private static string BuildKindLabel(PackFileContainerType containerType, bool isReadOnly)
        {
            var typeText = containerType switch
            {
                PackFileContainerType.SystemFolder => "System Folder",
                _ => containerType.ToString()
            };
            return isReadOnly ? $"[{typeText}, Read Only]" : $"[{typeText}]";
        }

        public string Header { get; set; }
        public string Path { get; set; }

        public ICommand Command { get; }
    }
}
