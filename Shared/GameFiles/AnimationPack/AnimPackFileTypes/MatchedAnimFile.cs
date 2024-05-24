using Shared.Core.ByteParsing;
using Shared.Core.Misc;
using Shared.GameFormats.DB;

namespace Shared.GameFormats.AnimationPack.AnimPackFileTypes
{
    public class MatchedAnimFile : IAnimationPackFile
    {
        public AnimationPackFile Parent { get; set; }
        public string FileName { get; set; }
        public bool IsUnknownFile { get; set; } = false;
        public NotifyAttr<bool> IsChanged { get; set; } = new NotifyAttr<bool>(false);

        uint _version;
        public List<MatchedAnimationTableEntry> Entries { get; set; } = new List<MatchedAnimationTableEntry>();

        public MatchedAnimFile(string fileName, byte[] bytes)
        {
            FileName = fileName;
            if (bytes != null)
                CreateFromBytes(bytes);
        }

        public void CreateFromBytes(byte[] bytes)
        {
            var chunk = new ByteChunk(bytes);
            _version = chunk.ReadUInt32();
            var count = chunk.ReadUInt32();
            Entries.Clear();
            for (var i = 0; i < count; i++)
            {
                var entry = new MatchedAnimationTableEntry();
                entry.AttackTable = new IntArrayTable(chunk);
                entry.AttackUnknown0 = chunk.ReadInt32();
                entry.AttackUnknown1 = chunk.ReadInt32();
                entry.AttackUnknown2 = chunk.ReadInt32();
                entry.AttackAnimation = chunk.ReadString();
                entry.MountAnimation = chunk.ReadString();
                entry.DefenceTable = new IntArrayTable(chunk);
                entry.DefenceUnknown0 = chunk.ReadInt32();
                entry.DefenceUnknown1 = chunk.ReadInt32();
                entry.DefenceUnknown2 = chunk.ReadInt32();
                entry.DefenceAnimation = chunk.ReadString();
                entry.Unknown_alwaysEmpty = chunk.ReadString();

                Entries.Add(entry);
            }

            var attackSize = Entries.Select(x => x.AttackTable.Data.Count).Distinct().ToList();  // 2
            var attackValues = Entries.SelectMany(x => x.AttackTable.Data).Distinct().ToList();  // 0, 1, 3, 5, 6 

            var attack0 = Entries.Select(x => x.AttackUnknown0).Distinct().ToList();  // 0, 1, 5 
            var attack1 = Entries.Select(x => x.AttackUnknown1).Distinct().ToList();  // 0
            var attack2 = Entries.Select(x => x.AttackUnknown2).Distinct().ToList();  // 1,2,3,4,5,8

            var defenceSize = Entries.Select(x => x.DefenceTable.Data.Count).Distinct().ToList();  // 2
            var defenceValues = Entries.SelectMany(x => x.DefenceTable.Data).Distinct().ToList();  // 0, 1, 3, 5, 6 


            var defence0 = Entries.Select(x => x.DefenceUnknown0).Distinct().ToList();  // 0, 1, 5 
            var defence1 = Entries.Select(x => x.DefenceUnknown1).Distinct().ToList();  // 0
            var defence2 = Entries.Select(x => x.DefenceUnknown2).Distinct().ToList();  // 1,2,3,4,5,8

            var mount = Entries.Select(x => x.MountAnimation).Distinct().ToList();  // 1,2,3,4,5,8
            var last = Entries.Select(x => x.Unknown_alwaysEmpty).Distinct().ToList();  // 1,2,3,4,5,8

        }

        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }
    }

    public class MatchedAnimationTableEntry
    {
        public IntArrayTable AttackTable { get; set; }
        public int AttackUnknown0 { get; set; }
        public int AttackUnknown1 { get; set; }
        public int AttackUnknown2 { get; set; }
        public string AttackAnimation { get; set; }
        public string MountAnimation { get; set; }
        public IntArrayTable DefenceTable { get; set; }
        public int DefenceUnknown0 { get; set; }
        public int DefenceUnknown1 { get; set; }
        public int DefenceUnknown2 { get; set; }
        public string DefenceAnimation { get; set; }
        public string Unknown_alwaysEmpty { get; set; }

        public MatchedAnimationTableEntry(/*ByteChunk data*/)
        {
            //AttackTable = new IntArrayTable(data);
            //AttackUnknown0 = data.ReadInt32();
            //AttackUnknown1 = data.ReadInt32();
            //AttackUnknown2 = data.ReadInt32();
            //AttackAnimation = data.ReadString();
            //MountAnimation = data.ReadString();
            //DefenceTable = new IntArrayTable(data);
            //DefenceUnknown0 = data.ReadInt32();
            //DefenceUnknown1 = data.ReadInt32();
            //DefenceUnknown2 = data.ReadInt32();
            //DefenceAnimation = data.ReadString();
            //Unknown_alwaysEmpty = data.ReadString();
        }
    }
}
