namespace Shared.GameFormats.AnimationMeta.Parsing
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
