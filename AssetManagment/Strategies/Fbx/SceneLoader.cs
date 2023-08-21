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
            
            // TODO: RE-ENABLE execption handling
            //try
            //{
                fbxSceneLoader = FBXSeneLoaderServiceDLL.CreateSceneFBX(fileName);
                var ptrNativeScene = FBXSeneLoaderServiceDLL.ProcessAndFillScene(fbxSceneLoader);
                var newSceneContainter = SceneMarshaller.ToManaged(ptrNativeScene);                  

                return newSceneContainter;
            //}
            //finally
            {
                if (fbxSceneLoader != IntPtr.Zero)
                    FBXSeneLoaderServiceDLL.DeleteBaseObj(fbxSceneLoader);
            }
        }

        // This does not belong here. Should be part of the scene, someone else should decide what do to with the data
        //private static void SetSkeletonBoneNames(IntPtr fbxSceneLoader, PackFileService pfs, out string out_SkeletonName)
        //{
        //    var skeletonNamePtr = FBXSeneLoaderServiceDLL.GetSkeletonNameFromScene(fbxSceneLoader);
        //
        //    out_SkeletonName = "";
        //
        //    if (skeletonNamePtr != IntPtr.Zero)
        //    {
        //        string skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);
        //        if (skeletonName == null)
        //        {
        //            throw new Exception("marshalling error");
        //        }
        //        else if (skeletonName == "")
        //        {
        //            return;
        //        }
        //
        //        string skeletonPath = $"animations/skeletons/{skeletonName}.anim";
        //        var packFile = pfs.FindFile(skeletonPath);
        //
        //        if (packFile == null)
        //        {
        //            out_SkeletonName = "";
        //            _logger.Warning("MODEL IMPORT ERROR: could not find skeleton .ANIM file, make sure you have the correct game selected");
        //            return;
        //        }
        //
        //        var animFile = AnimationFile.Create(packFile.DataSource.ReadDataAsChunk());
        //        if (animFile != null)
        //        {
        //            FBXSeneLoaderServiceDLL.ClearBoneNames(fbxSceneLoader);
        //
        //            foreach (var bone in animFile.Bones)
        //                FBXSeneLoaderServiceDLL.AddBoneName(fbxSceneLoader, bone.Name, bone.Name.Length);
        //
        //            out_SkeletonName = skeletonName; // output a skeleton name only when a skeleton is sucessfully loaded
        //        }
        //    };
        //}
    }
}
