using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using System;

namespace AssetManagement.Strategies.Fbx
{
    public class SceneLoader
    {
        public static SceneContainer LoadScene(string fileName)
        {
            IntPtr fbxSceneLoader = IntPtr.Zero;


            try
            {
                fbxSceneLoader = FBXSeneLoaderServiceDLL.CreateSceneFBX(fileName);
                var ptrNativeScene = FBXSeneLoaderServiceDLL.ProcessAndFillScene(fbxSceneLoader);
                var newSceneContainter = SceneMarshaller.ToManaged(ptrNativeScene);                  

                return newSceneContainter;
            }
            finally
            {
                if (fbxSceneLoader != IntPtr.Zero)
                    FBXSeneLoaderServiceDLL.DeleteBaseObj(fbxSceneLoader);
            }
        }   
    }
}
