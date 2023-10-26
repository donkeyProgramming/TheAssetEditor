// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace CommonControls.Interfaces.AssetManagement
{

    public class FileExtentions
    {
        public const string RMVV2 = "rigid_mode_v2";
        public const string VWSMODEL = "wsmodel";
        public const string VMD = "wsmodel";
        public const string OBJ = "obj";
        public const string FBX = "FBX";
    }

    
    public class AssetManagerFbxExport : IAssetManager
    {
        public AssetManagerData ManageAsset(AssetManagerData inputData)
        {
            // do the stuff
            return null;
        }

        public string[] Formats => new string[] { FileExtentions.RMVV2, FileExtentions.VWSMODEL, FileExtentions.VMD };
        public string[] InputFormats => new string[] { FileExtentions.FBX, FileExtentions.OBJ };
    }

    public interface IAssetManagerProvider
    {
        List<IAssetManager> GetAllIAssetManagers();
        T GetAssetManager<T>() where T : IAssetManager;
        IAssetManager GetAssetManager(string format);
    }

    //public class AssetManagerProvider : IAssetManagerProvider
    //{
    //    private readonly IEnumerable<IAssetManager> _importers;

    //    public AssetManagerProvider(IEnumerable<IAssetManager> importers)
    //    {
    //        _importers = importers;
    //    }

    //    public List<IAssetManager> GetAllIAssetManagers() => _importers.ToList();

    //    public T GetAssetManager<T>() where T : IAssetManager
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public T GetAssetManagerDataType<T, INPUT_DATA>(INPUT_DATA inputData) where T : IAssetManager
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IAssetManager GetAssetManager(string inputFormat, string outputFormat)
    //    {
    //        var importersMatchingInput = _importers.Where(x => IsValidInput(inputFormat, x));
    //        if (importersMatchingInput == null)
    //            throw new Exception($"No importer found for {inputFormat}");

    //        // check if the desired exporter exists
    //        var importerMatchingBoth = importersMatchingInput.Where(x => IsValidOutput(outputFormat, x));
    //        if (importerMatchingBoth == null)
    //            throw new Exception($"No exporter found for {outputFormat}");

    //        return importerMatchingBoth.FirstOrDefault();
    //    }

    //    bool IsValidOutput(string outputFormat, IAssetManager importer)
    //    {
    //        var resOut = importer.Formats.Any(x => x.Equals(outputFormat, StringComparison.InvariantCultureIgnoreCase));
    //        return resOut;
    //    }
    //}
};
