using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkActorMixer_v136 : HircItem, ICAkActorMixer
    {
        public NodeBaseParams NodeBaseParams { get; set; }
        public Children Children { get; set; }
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams = NodeBaseParams.Create(chunk);
            Children = Children.Create(chunk);
        }

        public override void UpdateSectionSize()
        {
            SectionSize = BnkChunkHeader.HeaderByteSize + Children.GetSize() + NodeBaseParams.GetSize() - 4;
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.GetAsByteArray());
            memStream.Write(Children.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkActorMixer_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public List<uint> GetChildren() => Children.ChildIdList;
    }
}
