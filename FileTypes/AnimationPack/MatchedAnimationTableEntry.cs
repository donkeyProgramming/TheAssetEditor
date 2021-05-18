using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.AnimationPack
{
    public class MatchedAnimationTableEntry
    {
        public class UnknownIntArrayTable
        {
            public List<int> Data { get; set; }
            public UnknownIntArrayTable(ByteChunk data)
            {
                var count = data.ReadInt32();
                Data = new List<int>(count);
                for (int i = 0; i < count; i++)
                    Data.Add(data.ReadInt32());
            }
        }

        public UnknownIntArrayTable AttackTable { get; set; }
        public int AttackUnknown0 { get; set; }
        public int AttackUnknown1 { get; set; }
        public int AttackUnknown2 { get; set; }
        public string AttackAnimation { get; set; }
        public string MountAnimation { get; set; }
        public UnknownIntArrayTable DefenceTable { get; set; }
        public int DefenceUnknown0 { get; set; }
        public int DefenceUnknown1 { get; set; }
        public int DefenceUnknown2 { get; set; }
        public string DefenceAnimation { get; set; }
        public string Unknown_alwaysEmpty { get; set; }

        public MatchedAnimationTableEntry(ByteChunk data)
        {
            AttackTable = new UnknownIntArrayTable(data);
            AttackUnknown0 = data.ReadInt32();
            AttackUnknown1 = data.ReadInt32();
            AttackUnknown2 = data.ReadInt32();
            AttackAnimation = data.ReadString();
            MountAnimation = data.ReadString();
            DefenceTable = new UnknownIntArrayTable(data);
            DefenceUnknown0 = data.ReadInt32();
            DefenceUnknown1 = data.ReadInt32();
            DefenceUnknown2 = data.ReadInt32();
            DefenceAnimation = data.ReadString();
            Unknown_alwaysEmpty = data.ReadString();
        }
    }
}
