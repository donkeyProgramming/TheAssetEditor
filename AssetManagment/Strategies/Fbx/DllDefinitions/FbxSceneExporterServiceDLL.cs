// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Geometry.Strategies.Fbx.DllDefinitions
{
    public class FBXSeneExporterServiceDLL
    {
        const string dllFileName = "FBXWrapperNative.dll";

        /// <summary>
        /// Make a new Native Exporter, and return pointer to it
        /// The returned pointer should be manually deleted
        /// </summary>        
        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MakeEmptyExporter();

        /// <summary>
        /// Retrives the SceneContainer from the Export
        /// Should NOT be manually deleted
        /// </summary>        
        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetNativeSceneContainer(IntPtr ptrExporter);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SaveToDisk(IntPtr ptrExporter, string path);
    }
}
