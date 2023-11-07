namespace CommonControls.Interfaces.AssetManagement
{
    /// <summary>
    /// Asset Export Data - including all the data types the exporter might use          


    public interface IAssetExporter
    {
        byte[] ExportAsset(AssetManagerData inputData, AssetConfigData config = null);
        string[] Formats { get; }
    }
}
