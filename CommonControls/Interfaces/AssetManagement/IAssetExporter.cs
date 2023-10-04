using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.WsModel;


namespace CommonControls.Interfaces.AssetManagement
{
    /// <summary>
    /// Asset Export Data - including all the data types the exporter might use          
   
    public interface IAssetExporter
    {
        byte[] ExportAsset(AssetParamData inputData);
        string[] Formats { get; }
    }
}
