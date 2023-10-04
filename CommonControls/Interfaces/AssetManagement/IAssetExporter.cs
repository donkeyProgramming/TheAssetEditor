namespace CommonControls.Interfaces.AssetManagement
{    
    public interface IAssetExporter
    {
        /// <summary>
        /// Exports an asset to binary data
        /// TODO:       Ole: what if the FBX SDK only can save directly to disk? (I can't recall)        
        /// </summary>
        /// <param name="inputData">Container for all data an export op might need</param>
        /// <returns></returns>
        byte[] ExportAsset(AssetParamData inputData);
                
        string[] Formats { get; }
    }
}
