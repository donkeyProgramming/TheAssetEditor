using AssetManagement.Marshalling;
using AssetManagement.GenericFormats;
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
                fbxSceneLoader = FBXSeneImporterServiceDLL.CreateSceneFBX(fileName);
                var ptrNativeScene = FBXSeneImporterServiceDLL.ProcessAndFillScene(fbxSceneLoader);
                var newSceneContainter = SceneMarshaller.CopyToManaged(ptrNativeScene);

                return newSceneContainter;
            }
            finally
            {
                if (fbxSceneLoader != IntPtr.Zero)
                    FBXSeneImporterServiceDLL.DeleteBaseObj(fbxSceneLoader);
            }
        }
    }

    // TODO: move to own .cs file??
    public class SceneExporter
    {
        public static void ExportScene(SceneContainer sourceScene, string fileName)
        {
            var ptrNativeExporter = IntPtr.Zero;
            var ptrNativeceneContainer = IntPtr.Zero;
                        
            try
            {
                ptrNativeExporter = FBXSeneExporterServiceDLL.MakeEmptyExporter();
                ptrNativeceneContainer = FBXSeneExporterServiceDLL.GetNativeSceneContainer(ptrNativeExporter);

                SceneMarshaller.CopyToNative(ptrNativeceneContainer, sourceScene);
                FBXSeneExporterServiceDLL.SaveToDisk(ptrNativeExporter, fileName);
            }
            
            finally
            {
                if (ptrNativeExporter != IntPtr.Zero)
                    FBXSeneImporterServiceDLL.DeleteBaseObj(ptrNativeExporter);
            }

        }
    }
}
