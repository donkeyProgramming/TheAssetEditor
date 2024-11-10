using System.Text;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;

namespace Shared.GameFormats.AnimationPack
{
    [Serializable]
    public class AnimationSlotType
    {
        public int Id { get; set; }
        public string Value { get; set; }

        public AnimationSlotType(int id, string value)
        {
            Id = id;
            Value = value.ToUpper();
        }

        public AnimationSlotType()
        { }

        public AnimationSlotType Clone()
        {
            return new AnimationSlotType(Id, Value);
        }

        public override string ToString()
        {
            return $"{Value}[{Id}]";
        }
    }

   
}

