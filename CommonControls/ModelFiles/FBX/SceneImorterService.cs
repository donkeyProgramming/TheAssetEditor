using System;
using Serilog;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CommonControls.Services;
using CommonControls.FileTypes.Animation;
using CommonControls.ModelFiles.Mesh;
using CommonControls.ModelFiles.Mesh.Native;
using CommonControls.Common;

namespace CommonControls.ModelFiles.FBX
{
    public class SceneImorterService
    {
        static private ILogger _logger = Logging.Create<SceneImorterService>();

        public static SceneContainer CreateSceneFromFBX(string fileName, PackFileService pfs, out string skeletonName)
        {

            //var DLL = Assembly.LoadFile("FBXWrapperNative.dll");

            var newScene = new SceneContainer();
            var fbxSceneLoader = FBXSeneLoaderServiceDLL.CreateSceneFBX(fileName); // inits the FBX, no vertex proecsssing yet

            // find skeleton
            SetSkeletonBoneNames(pfs, fbxSceneLoader, out skeletonName);
                        
            // process the whole scene and copy to "SceneContainer"
            var ptrNativeScene = FBXSeneLoaderServiceDLL.ProcessAndFillScene(fbxSceneLoader);
            SceneMarshaller.ToManaged(ptrNativeScene, newScene);

            // delete C++ pointer
            FBXSeneLoaderServiceDLL.DeleteBaseObj(fbxSceneLoader);

            return newScene;
        }

        private static void SetSkeletonBoneNames(PackFileService pfs, IntPtr fbxSceneLoader, out string outSkeletonName)
        {
                var skeletonNamePtr = FBXSeneLoaderServiceDLL.GetSkeletonNameFromScene(fbxSceneLoader);

            outSkeletonName = "";

            if (skeletonNamePtr != IntPtr.Zero)
            {
                string skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);
                if (skeletonName == null)
                {
                    throw new Exception("marshalling error");
                }
                else if (skeletonName == "")
                {
                    return;
                }

                string skeletonPath = $"animations/skeletons/{skeletonName}.anim";
                var packFile = pfs.FindFile(skeletonPath);

                if (packFile == null)
                {
                    outSkeletonName = "";
                    _logger.Warning("MODEL IMPORT ERROR: could not find skeleton .ANIM file, make sure you have the correct game selected");
                    return;
                }

                var animFile = AnimationFile.Create(packFile.DataSource.ReadDataAsChunk());

                if (animFile != null)
                {
                    FBXSeneLoaderServiceDLL.ClearBoneNames(fbxSceneLoader);

                    foreach (var bone in animFile.Bones)
                        FBXSeneLoaderServiceDLL.AddBoneName(fbxSceneLoader, bone.Name, bone.Name.Length);

                    outSkeletonName = skeletonName; // output a skeleton name only when a skeleton is sucessfully loaded
                }
            };
        }
    }
    public class SceneMarshaller
    {
     
        public static void ToManaged(IntPtr ptrFbxSceneContainer, SceneContainer destScene)
        {
            destScene.Meshes = GetAllPackedMeshes(ptrFbxSceneContainer);
            /*
            - destScene.Bones = GetAllBones();
            - destScene.Animations = GetAllBones();
            - etc, comming soon
            */
        }

        public static PackedCommonVertex[]? GetPackesVertices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pVerticesPtr = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerDll.GetPackedVertices(fbxContainer, meshIndex, out pVerticesPtr, out length);

            if (pVerticesPtr == IntPtr.Zero || length == 0)
            {
                return null;
            }

            PackedCommonVertex[] data = new PackedCommonVertex[length];
            for (int vertexIndex = 0; vertexIndex < length; vertexIndex++)
            {
                var ptr = Marshal.PtrToStructure(pVerticesPtr + vertexIndex * Marshal.SizeOf(typeof(PackedCommonVertex)), typeof(PackedCommonVertex));

                if (ptr != null)
                    data[vertexIndex] = (PackedCommonVertex)ptr;
            }

            return data;
        }

        public static ushort[]? GetIndices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pIndices = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerDll.GetIndices(fbxContainer, meshIndex, out pIndices, out length);

            if (pIndices == IntPtr.Zero || length == 0)
                return null;

            var indexArray = new ushort[length];

            for (int indicesIndex = 0; indicesIndex < length; indicesIndex++)
            {
                indexArray[indicesIndex] = (ushort)Marshal.PtrToStructure(pIndices + indicesIndex * Marshal.SizeOf(typeof(ushort)), typeof(ushort));
            }
            return indexArray;
        }

        public static PackedMesh GetPackedMesh(IntPtr fbxContainer, int meshIndex)
        {
            var indices = GetIndices(fbxContainer, meshIndex);
            var vertices = GetPackesVertices(fbxContainer, meshIndex);

            IntPtr namePtr = FBXSCeneContainerDll.GetMeshName(fbxContainer, meshIndex);
            var tempName = Marshal.PtrToStringUTF8(namePtr);

            if (vertices == null || indices == null || tempName == null)
                throw new Exception("Params/Input Data Invalid: Vertices, Indices or Name == null");

            PackedMesh packedMesh = new PackedMesh();
            packedMesh.Vertices = new List<PackedCommonVertex>();
            packedMesh.Indices = new List<ushort>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);
            packedMesh.Name = tempName;

            return packedMesh;
        }

        static public List<PackedMesh> GetAllPackedMeshes(IntPtr fbxSceneContainer)
        {
            List<PackedMesh> meshList = new List<PackedMesh>();
            var meshCount = FBXSCeneContainerDll.GetMeshCount(fbxSceneContainer);

            for (int i = 0; i < meshCount; i++)
            {
                meshList.Add(GetPackedMesh(fbxSceneContainer, i));
            }

            return meshList;
        }

    }


    // TODO: move to scene.cs
    public class FBXSCeneContainerDll
    {
        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetPackedVertices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetIndices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeshCount(IntPtr ptrInstances);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMeshName(IntPtr ptrInstances, int meshIndex);
    }

    public class FBXSeneLoaderServiceDLL
    {
        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteBaseObj(IntPtr ptrInstances);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ProcessAndFillScene(IntPtr ptrFBXSceneLoaderService);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateSceneFBX(string path);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXSceneImporterService();

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXContainer();

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSkeletonNameFromScene(IntPtr ptrSceneLoader);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddBoneName(IntPtr ptrInstances, string boneName, int len);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearBoneNames(IntPtr ptrInstances);
    }
}
