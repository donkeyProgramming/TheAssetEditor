// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.RigidModel.Transforms
{
    [Serializable]
    public struct RmvVector4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public RmvVector4(float x, float y, float z, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public RmvVector4(float value = 0)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}, {W}";
        }

        public Vector4 ToVector4()
        {
            return new Vector4(X, Y, Z, W);
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }


        public Vector4 ToVector4(float w)
        {
            return new Vector4(X, Y, Z, w);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public RmvVector4 Clone() => new RmvVector4(X, Y, Z, W);
    }
}
