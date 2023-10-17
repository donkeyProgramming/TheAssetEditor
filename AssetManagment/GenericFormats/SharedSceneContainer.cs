// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.Marshalling;

// TODO: throw out or implement!
namespace AssetManagement.GenericFormats.MeshHandling
{
public class SharedSceneContainer
{
    private readonly IntPtr _ptrNativeSceneContainer;

    SharedSceneContainer(IntPtr ptrNativeSceneContainer)
    {
        _ptrNativeSceneContainer = ptrNativeSceneContainer;
    }

    List<PackedMesh> Meshes
    {
        set  { /*SceneMarshaller.SetAllPackedMeshes(_ptrNativeSceneContainer, value)*/;  }

        get { return SceneMarshaller.GetAllPackedMeshes(_ptrNativeSceneContainer); }
    }



        static void a()
        {
            var scene = new SharedSceneContainer(IntPtr.Zero);

            var meshe = scene.Meshes[0];
        }

    }
}

