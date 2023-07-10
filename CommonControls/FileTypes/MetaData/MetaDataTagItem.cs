// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonControls.FileTypes.MetaData
{

    public class MetaDataTagItem
    {
        public class TagData
        {
            public TagData(byte[] bytes, int start, int size)
            {
                Bytes = bytes;
                Start = start;
                Size = size;
            }

            public byte[] Bytes { get; set; }
            public int Start { get; set; }
            public int Size { get; set; }
        }

        public string Name { get; set; } = ""; // Only name, no _v10 stuff here. Used for saving
        public TagData DataItem { get; set; }
    }
}
