using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Filetypes.ByteParsing;

namespace CommonControls.Interfaces.AssetManagement
{
    public interface IAssetImporter
    {
        PackFile ImportAsset(string diskFilePath);
        string[] Formats { get; }
    }

    public interface IAssetExporter
    {
        byte[] ExportAsset(PackFile packFile);
        string[] Formats { get; }
    }
}
