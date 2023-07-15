using CommonControls.FileTypes.PackFiles.Models;

namespace CommonControls.Interfaces.AssetManagement
{
    public interface IAssetImporter
    {
        PackFile ImportAsset(string meshPath);
        string[] Formats { get; }
    }
}
