using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkActorMixer_V112 : HircItem, ICAkActorMixer
    {
        public NodeBaseParams_V112 NodeBaseParams { get; set; } = new NodeBaseParams_V112();
        public Children_V112 Children { get; set; } = new Children_V112();

        protected override void ReadData(ByteChunk chunk)
        {
            NodeBaseParams.ReadData(chunk);
            Children.ReadData(chunk);
        }

        public override byte[] WriteData()
        {
            using var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.WriteData());
            memStream.Write(Children.WriteData());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkActorMixer_V112();
            sanityReload.ReadHirc(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            SectionSize = idSize + Children.GetSize() + NodeBaseParams.GetSize();
        }

        public List<uint> GetChildren() => Children.ChildIds;
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
    }
}
