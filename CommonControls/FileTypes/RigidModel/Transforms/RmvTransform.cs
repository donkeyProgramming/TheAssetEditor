// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CommonControls.FileTypes.RigidModel.Transforms
{
    [Serializable]
    public struct RmvTransform
    {
        public RmvVector3 Pivot;
        public RmvMatrix3x4 Matrix0;
        public RmvMatrix3x4 Matrix1;
        public RmvMatrix3x4 Matrix2;

        public bool IsIdentityPivot()
        {
            if (!Pivot.IsAllZero())
                return false;
            return true;
        }

        public bool IsIdentityMatrices()
        {
            if (!Matrix0.IsIdentity())
                return false;

            if (!Matrix1.IsIdentity())
                return false;

            if (!Matrix2.IsIdentity())
                return false;

            return true;
        }

    }
}
