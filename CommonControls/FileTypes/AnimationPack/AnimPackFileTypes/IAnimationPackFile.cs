using CommonControls.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.AnimationPack.AnimPackFileTypes
{
    public interface IAnimationPackFile
    {
        // Game version
        AnimationPackFile Parent { get; set; }

        string FileName { get; set; }
        public bool IsUnknownFile{ get; set; }
        public NotifyAttr<bool> IsChanged { get; set; }

        void CreateFromBytes(byte[] bytes);
        byte[] ToByteArray();
    }
}
