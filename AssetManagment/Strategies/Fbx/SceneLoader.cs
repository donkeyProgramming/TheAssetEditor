using AssetManagement.GenericFormats;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using AssetManagement.Strategies.Fbx.Models;
using AssetManagement.Strategies.Fbx.ViewModels;
using CommonControls.Common;
using Serilog;
using System;

namespace AssetManagement.Strategies.Fbx
{
    public class SceneLoader
    {
        static private ILogger _logger = Logging.Create<SceneLoader>();

        public static SceneContainer LoadScene(string fileName)
        {
            IntPtr fbxSceneLoader = IntPtr.Zero;
            
            try
            {
                fbxSceneLoader = FBXSeneLoaderServiceDLL.CreateSceneFBX(fileName);

                var ptrNativeScene = FBXSeneLoaderServiceDLL.ProcessAndFillScene(fbxSceneLoader);
                var managedScene = SceneMarshallerToManaged.ToManaged(ptrNativeScene);
                return managedScene;
            }
            finally
            {
                if (fbxSceneLoader != IntPtr.Zero)
                    FBXSeneLoaderServiceDLL.DeleteBaseObj(fbxSceneLoader);
            }
        }
    }
}
