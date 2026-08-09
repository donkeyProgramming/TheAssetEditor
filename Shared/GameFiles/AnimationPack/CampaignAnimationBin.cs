using Shared.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.AnimationPack
{
    public class CampaignAnimationBin
    {
        public string Reference { get; set; }
        public string SkeletonName { get; set; }
        public int Version { get; set; }
        public List<StatusItem> Status { get; set; } = new List<StatusItem>();

        public interface ICampaignAnimationBinEntry
        {
            byte[] ToBytes(ref List<string> stringTable);
        }

        public class StatusItem
        {
            public string Name { get; set; }

            public List<PersistentMeta> PersitantMetaData { get; set; }
            public List<PersistentMeta_Pose> Poses { get; set; }
            public List<PersistentMeta_Dock> Docks { get; set; }
            public List<AnimationEntry> Idle { get; set; }
            public List<PortholeEntry> Porthole { get; set; }
            public List<AnimationEntry> Selection { get; set; }
            public List<TransitionEntry> Transitions { get; set; }
            public List<ActionEntry> Action { get; set; }
            public List<MissingType> Unk1 { get; set; }
            public List<UnknownEntry> Unknown { get; set; }
            public List<MissingType> Unk3 { get; set; }
            public List<LocomotionEntry> Locomotion { get; set; }
        }

        public class AnimationEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string MetaFile { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public float Weight { get; set; }

            public static AnimationEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new AnimationEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.MetaFile = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.Weight = byteChunk.ReadSingle();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(MetaFile, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.Write(Weight, ByteParsers.Single);
                return chuck.GetBytes();
            }
        }

        public class TransitionEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public string TransitionTo { get; set; }

            public static TransitionEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new TransitionEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.TransitionTo = byteChunk.ReadStringTableIndex(stringTable);
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.WriteStringTableIndex(TransitionTo, ref stringTable);
                return chuck.GetBytes();
            }
        }

        public class PersistentMeta : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }

            public static PersistentMeta FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                return chuck.GetBytes();
            }
        }

        public class PersistentMeta_Pose : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public int PoseId { get; set; }

            public static PersistentMeta_Pose FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta_Pose();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.PoseId = byteChunk.ReadInt32();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.Write(PoseId, ByteParsers.Int32);
                return chuck.GetBytes();
            }
        }

        public class PersistentMeta_Dock : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public string Dock { get; set; }

            public static PersistentMeta_Dock FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta_Dock();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.Dock = byteChunk.ReadStringTableIndex(stringTable).ToUpper();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.WriteStringTableIndex(Dock.ToUpper(), ref stringTable, false);
                return chuck.GetBytes();
            }
        }

        /// <summary>Structurally identical to <see cref="AnimationEntry"/>. Previously used generic
        /// Value0-5 names because its shape hadn't been identified; confirmed against vanilla data.</summary>
        public class PortholeEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public float Weight { get; set; }

            public static PortholeEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PortholeEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.Weight = byteChunk.ReadSingle();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.Write(Weight, ByteParsers.Single);
                return chuck.GetBytes();
            }
        }

        public class ActionEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string Meta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public string ActionType { get; set; }
            public int ActionId { get; set; }

            /// <summary>Always false across ~1700 surveyed vanilla rows - purpose unknown.</summary>
            public bool Unknown { get; set; }

            public static ActionEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new ActionEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.Meta = byteChunk.ReadStringTableIndex(stringTable);

                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();

                output.ActionType = byteChunk.ReadStringTableIndex(stringTable);
                output.ActionId = byteChunk.ReadInt32();
                output.Unknown = byteChunk.ReadBool();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(Meta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.WriteStringTableIndex(ActionType, ref stringTable);
                chuck.Write(ActionId, ByteParsers.Int32);
                chuck.Write(Unknown, ByteParsers.Bool);
                return chuck.GetBytes();
            }
        }

        public class LocomotionEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }

            /// <summary>Surveyed values (0, 0.1-0.7, 1, 2) match the BlendTime pattern of every
            /// other entry type, not a 0-1 blend weight - renamed from the original "Weight".</summary>
            public float BlendTime { get; set; }

            /// <summary>Always 1.0 in vanilla (660/661); doesn't appear to be used.</summary>
            public float ModelScale { get; set; }

            /// <summary>Not a walk-vs-run distance threshold as the name suggests - values overlap
            /// between walk/run/fly buckets, and MinDistance sometimes exceeds Distance for the same
            /// clip. Probably a root-motion distance range for matching playback to movement speed,
            /// but unconfirmed.</summary>
            public float DistanceTraveled { get; set; }
            public float DistanceMinTravled { get; set; }

            public static LocomotionEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new LocomotionEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.ModelScale = byteChunk.ReadSingle();
                output.DistanceTraveled = byteChunk.ReadSingle();
                output.DistanceMinTravled = byteChunk.ReadSingle();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.Write(ModelScale, ByteParsers.Single);
                chuck.Write(DistanceTraveled, ByteParsers.Single);
                chuck.Write(DistanceMinTravled, ByteParsers.Single);
                return chuck.GetBytes();
            }
        }

        /// <summary>Rare - only 6 occurrences across 661 vanilla files, all one-off poses for rider
        /// or vehicle-driver characters. Same shape as <see cref="AnimationEntry"/> plus a trailing
        /// int (<see cref="Value"/>), which was 0 in every occurrence found.</summary>
        public class UnknownEntry : ICampaignAnimationBinEntry
        {
            public string Animation { get; set; }
            public string Type { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float BlendTime { get; set; }
            public float Weight { get; set; }
            public int Value { get; set; }

            public static UnknownEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new UnknownEntry();
                output.Animation = byteChunk.ReadStringTableIndex(stringTable);
                output.Type = byteChunk.ReadStringTableIndex(stringTable);
                output.AnimationMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.SoundMeta = byteChunk.ReadStringTableIndex(stringTable);
                output.BlendTime = byteChunk.ReadSingle();
                output.Weight = byteChunk.ReadSingle();
                output.Value = byteChunk.ReadInt32();
                return output;
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                var chuck = new ChuckWriter();
                chuck.WriteStringTableIndex(Animation, ref stringTable);
                chuck.WriteStringTableIndex(Type, ref stringTable);
                chuck.WriteStringTableIndex(AnimationMeta, ref stringTable);
                chuck.WriteStringTableIndex(SoundMeta, ref stringTable);
                chuck.Write(BlendTime, ByteParsers.Single);
                chuck.Write(Weight, ByteParsers.Single);
                chuck.Write(Value, ByteParsers.Int32);
                return chuck.GetBytes();
            }
        }

        public class MissingType : ICampaignAnimationBinEntry
        {
            public static MissingType FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                throw new Exception("Unknown datatype");
            }

            public byte[] ToBytes(ref List<string> stringTable)
            {
                throw new Exception("Unknown datatype");
            }
        }
    }



    public class CampaignAnimationBinLoader
    {
        delegate T CreateEntryDelegate<T>(ByteChunk chung, string[] table);

        public static CampaignAnimationBin Load(ByteChunk data)
        {
            var outputFile = new CampaignAnimationBin();
            outputFile.Version = data.ReadInt32();

            if (outputFile.Version != 3)
                throw new Exception("Version error +" + outputFile.Version);

            var strTableOffset = data.ReadInt32();
            var strTableChunk = new ByteChunk(data.Buffer, strTableOffset);

            var stringTable = ReadStrTable(strTableChunk);

            var skeletonStrIndex = data.ReadInt32();                // 1
            var selfRefStringIndex = data.ReadInt32();              // 0
            var someValueAlwaysOne = data.ReadInt32();              // 1

            outputFile.Reference = stringTable[0];
            outputFile.SkeletonName = stringTable[1];

            var numStatuses = data.ReadInt32();
            for (var i = 0; i < numStatuses; i++)
            {
                var status = LoadStatus(data, stringTable);
                outputFile.Status.Add(status);
            }

            if (strTableOffset != data.Index)
                throw new Exception("Data left");

            return outputFile;
        }

        public static byte[] Write(CampaignAnimationBin bin, string fileName)
        {
            var dataWriter = new ChuckWriter();
            var stringTable = new List<string>();

            dataWriter.Write(1, ByteParsers.Int32);
            dataWriter.Write(0, ByteParsers.Int32);
            dataWriter.Write(1, ByteParsers.Int32);

            stringTable.Add(fileName);
            stringTable.Add(bin.SkeletonName);

            // Statuses
            dataWriter.Write(bin.Status.Count, ByteParsers.Int32);
            foreach (var status in bin.Status)
                WriteStatus(status, dataWriter, ref stringTable);

            var dataBytes = dataWriter.GetBytes();
            var finalWriter = new ChuckWriter();
            finalWriter.Write(bin.Version, ByteParsers.Int32);
            finalWriter.Write(dataBytes.Length + 8, ByteParsers.Int32);
            finalWriter.AddBytes(dataBytes);

            finalWriter.Write(stringTable.Count, ByteParsers.Int32);
            foreach (var str in stringTable)
                finalWriter.Write(str, ByteParsers.String);

            return finalWriter.GetBytes();
        }

        static CampaignAnimationBin.StatusItem LoadStatus(ByteChunk data, string[] strTable)
        {
            var statusName = data.ReadStringTableIndex(strTable);
            var currentStatus = new CampaignAnimationBin.StatusItem() { Name = statusName };

            if (statusName == "global") // Special case
            {
                currentStatus.PersitantMetaData = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta.FromChunck);
                currentStatus.Poses = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta_Pose.FromChunck);
                currentStatus.Docks = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta_Dock.FromChunck);
            }
            else
            {
                currentStatus.Idle = LoadSlots(data, strTable, CampaignAnimationBin.AnimationEntry.FromChunck);
                currentStatus.Porthole = LoadSlots(data, strTable, CampaignAnimationBin.PortholeEntry.FromChunck);
                currentStatus.Selection = LoadSlots(data, strTable, CampaignAnimationBin.AnimationEntry.FromChunck);
                currentStatus.Transitions = LoadSlots(data, strTable, CampaignAnimationBin.TransitionEntry.FromChunck);
                currentStatus.Action = LoadSlots(data, strTable, CampaignAnimationBin.ActionEntry.FromChunck);
                currentStatus.Unk1 = LoadSlots(data, strTable, CampaignAnimationBin.MissingType.FromChunck);    // Always zero
                currentStatus.Unknown = LoadSlots(data, strTable, CampaignAnimationBin.UnknownEntry.FromChunck); // What is this?
                currentStatus.Unk3 = LoadSlots(data, strTable, CampaignAnimationBin.MissingType.FromChunck);    // Always zero
                currentStatus.Locomotion = LoadSlots(data, strTable, CampaignAnimationBin.LocomotionEntry.FromChunck);
            }

            return currentStatus;
        }

        static List<T> LoadSlots<T>(ByteChunk data, string[] stringTable, CreateEntryDelegate<T> createEntryDelegate)
        {
            var numItems = data.ReadInt32();
            var output = new List<T>();
            for (var i = 0; i < numItems; i++)
            {
                var newItem = createEntryDelegate(data, stringTable);
                output.Add(newItem);
            }

            if (output.Count == 0)
                return null;

            return output;
        }

        static void WriteStatus(CampaignAnimationBin.StatusItem statusItem, ChuckWriter writer, ref List<string> stringTable)
        {
            writer.WriteStringTableIndex(statusItem.Name, ref stringTable);

            if (statusItem.Name == "global")
            {
                WriteSlot(statusItem.PersitantMetaData, writer, ref stringTable);
                WriteSlot(statusItem.Poses, writer, ref stringTable);
                WriteSlot(statusItem.Docks, writer, ref stringTable);
            }
            else
            {
                WriteSlot(statusItem.Idle, writer, ref stringTable);
                WriteSlot(statusItem.Porthole, writer, ref stringTable);
                WriteSlot(statusItem.Selection, writer, ref stringTable);
                WriteSlot(statusItem.Transitions, writer, ref stringTable);
                WriteSlot(statusItem.Action, writer, ref stringTable);
                WriteSlot(statusItem.Unk1, writer, ref stringTable);
                WriteSlot(statusItem.Unknown, writer, ref stringTable);
                WriteSlot(statusItem.Unk3, writer, ref stringTable);
                WriteSlot(statusItem.Locomotion, writer, ref stringTable);
            }
        }

        static void WriteSlot<T>(IEnumerable<T> entries, ChuckWriter writer, ref List<string> stringTable) where T : CampaignAnimationBin.ICampaignAnimationBinEntry
        {
            if (entries == null)
            {
                writer.Write(0, ByteParsers.Int32);
                return;
            }

            writer.Write(entries.Count(), ByteParsers.Int32);
            foreach (var entry in entries)
            {
                var bytes = entry.ToBytes(ref stringTable);
                writer.AddBytes(bytes);
            }
        }

        static string[] ReadStrTable(ByteChunk data)
        {
            var stringTable = new List<string>();
            var numTableEntires = data.ReadInt32();
            for (var i = 0; i < numTableEntires; i++)
                stringTable.Add(data.ReadString());
            return stringTable.ToArray();
        }
    }

    public static class AnimationCampaignBinHelper
    {
        public static void BatchProcess(List<PackFile> fileList)
        {
            var counter = 0;
            var loadErrors = new List<string>();
            var writeErrors = new List<string>();
            var reLoadErrors = new List<string>();
            var output = new List<CampaignAnimationBin>();
            foreach (var f in fileList)
            {

                // Load
                // -----------------
                var chunkf = f.DataSource.ReadDataAsChunk();
                CampaignAnimationBin loadedBin = null;

                try
                {
                    loadedBin = CampaignAnimationBinLoader.Load(chunkf);
                    output.Add(loadedBin);
                }
                catch (Exception e)
                {
                    loadErrors.Add(f.Name + " " + e.Message);
                }


                // Write
                // -----------------


                // Reload
                // -----------------


                counter++;
            }

            var x0 = loadErrors.Where(x => x.Contains("missing schema")).ToList();      // 52
            var x1 = loadErrors.Where(x => x.Contains("Index was outside")).ToList();
            var x2 = loadErrors.Where(x => x.Contains("Metadata error")).ToList();
            var x3 = loadErrors.Where(x => x.Contains("Sequence contains no elements")).ToList();
            var x4 = loadErrors.Where(x => x.Contains("Version error")).ToList();   // 1
            var x5 = loadErrors.Where(x => x.Contains("Unknown datatype")).ToList();   // 1

            var eTotal = x0.Count + x1.Count + x2.Count + x3.Count + x4.Count + x5.Count;     // 53 
            return;
        }
    }


}
