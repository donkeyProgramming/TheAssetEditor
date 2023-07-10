// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Common;

namespace CommonControls.FileTypes.AnimationPack.AnimPackFileTypes
{
    public interface IAnimationPackFile
    {
        // Game version
        AnimationPackFile Parent { get; set; }

        string FileName { get; set; }
        public bool IsUnknownFile { get; set; }
        public NotifyAttr<bool> IsChanged { get; set; }

        void CreateFromBytes(byte[] bytes);
        byte[] ToByteArray();
    }
}
