// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Geometry.Marshalling
{
    public class MarshalUtil
    {
        public static bool CopyArrayToUnmanaged<STRUCT_TYPE>(STRUCT_TYPE[] src, IntPtr ptrArrayAddress, int elementCount)
        {
            if (ptrArrayAddress == IntPtr.Zero || elementCount == 0)
            {
                return false;
            }

            for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                Marshal.StructureToPtr(src[elementIndex], ptrArrayAddress + elementIndex * Marshal.SizeOf(typeof(STRUCT_TYPE)), false);
            }

            return true;
        }

        public static STRUCT_TYPE[] CopyArrayFromUnmanaged<STRUCT_TYPE>(IntPtr ptrArrayAddress, int elementCount)
        {
            if (ptrArrayAddress == IntPtr.Zero || elementCount == 0)
            {
                return null;
            }

            var data = new STRUCT_TYPE[elementCount];
            for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
            {
                var ptrNewStruct = Marshal.PtrToStructure(ptrArrayAddress + elementIndex * Marshal.SizeOf(typeof(STRUCT_TYPE)), typeof(STRUCT_TYPE));

                if (ptrNewStruct != null)
                {
                    data[elementIndex] = (STRUCT_TYPE)ptrNewStruct;
                }
            }

            return data;
        }

    }
}
