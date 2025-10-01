using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using static Shared.GameFormats.Wwise.Hirc.ICAkDialogueEvent;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkGameSync_V136 : IAkGameSync
    {
        public uint GroupId { get; set; }
        public AkGroupType GroupType { get; set; }

        public uint GetSize()
        {
            var groupIdSize = ByteHelper.GetPropertyTypeSize(GroupId);
            var groupTypeSize = ByteHelper.GetPropertyTypeSize(GroupType);
            return groupIdSize + groupTypeSize;
        }

        public AkGameSync_V136 Clone()
        {
            return new AkGameSync_V136
            {
                GroupId = GroupId,
                GroupType = GroupType
            };
        }
    }
}
