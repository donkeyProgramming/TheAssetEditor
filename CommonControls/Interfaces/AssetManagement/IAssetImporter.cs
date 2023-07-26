using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;

namespace CommonControls.Interfaces.AssetManagement
{
    public interface IAssetImporter
    {
        PackFile ImportAsset(string diskFilePath);
        string[] Formats { get; }
    }
}
