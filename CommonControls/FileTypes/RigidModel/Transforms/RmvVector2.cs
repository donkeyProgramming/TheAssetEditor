// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;

namespace CommonControls.FileTypes.RigidModel.Transforms
{
    public struct RmvVector2
    {
        public float X;
        public float Y;

        public RmvVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
