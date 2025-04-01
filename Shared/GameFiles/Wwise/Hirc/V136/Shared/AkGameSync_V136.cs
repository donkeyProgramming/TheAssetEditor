using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkGameSync_V136
    {
        public uint GroupID { get; set; }
        public AkGroupType GroupType { get; set; }

        public uint GetSize()
        {
            var groupIdSize = ByteHelper.GetPropertyTypeSize(GroupID);
            var groupTypeSize = ByteHelper.GetPropertyTypeSize(GroupType);
            return groupIdSize + groupTypeSize;
        }
    }
}
