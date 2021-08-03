using Common;
using Common.GameInformation;
using Filetypes.ByteParsing;
using FileTypes.DB;
using FileTypes.PackFiles.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FileTypes.AnimationPack
{
    public class CampaignAnimationBin
    {
        public string Reference { get; set; }
        public string SkeletonName { get; set; }
        public int Version { get; set; }
        public List<StatusItem> Status { get; set; } = new List<StatusItem>();

        public class StatusItem
        {
            public string Name { get; set; }

            public PersistentMeta PersitantMetaData { get; set; }
            public List<PersistentMeta_Pose> Poses { get; set; }
            public List<PersistentMeta_Dock> Docks { get; set; } 

            public List<AnimationEntry> Idle { get; set; } 
            public List<PortholeEntry> Porthole { get; set; }
            public List<AnimationEntry> Selection { get; set; }
            public List<MissingType> Unk0 { get; set; }
            public List<ActionEntry> Action { get; set; } 
            public List<MissingType> Unk1 { get; set; }
            public List<UnknownEntry> Unkown { get; set; }
            public List<MissingType> Unk3 { get; set; } 
            public List<LocomotionEntry> Locomotion { get; set; }
        }

        public class AnimationEntry
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public string Type { get; set; }
            public float BlendTime { get; set; }
            public float Weight { get; set; }

            public static AnimationEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new AnimationEntry();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Type = stringTable[byteChunk.ReadInt32()];
                output.BlendTime = byteChunk.ReadSingle();
                output.Weight = byteChunk.ReadSingle();
                return output;
            }
        }

        public class PersistentMeta
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public string Type { get; set; }
            public float BlendTime { get; set; }
            public int NumPoses { get; set; }
            public int NumDocks { get; set; }

            public static PersistentMeta FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Type = stringTable[byteChunk.ReadInt32()];
                output.BlendTime = byteChunk.ReadSingle();
                output.NumPoses = byteChunk.ReadInt32();
                output.NumDocks = byteChunk.ReadInt32();
                return output;
            }
        }

        public class PersistentMeta_Pose
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float Weight { get; set; }
            public float BlendTime { get; set; }
            public int PoseId { get; set; }

            public static PersistentMeta_Pose FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta_Pose();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Weight = byteChunk.ReadSingle();
                output.BlendTime = byteChunk.ReadSingle();
                output.PoseId = byteChunk.ReadInt32();
                return output;
            }
        }

        public class PersistentMeta_Dock
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public float Weight { get; set; }
            public float BlendTime { get; set; }
            public string Dock { get; set; }

            public static PersistentMeta_Dock FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PersistentMeta_Dock();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Weight = byteChunk.ReadSingle();
                output.BlendTime = byteChunk.ReadSingle();
                output.Dock = stringTable[byteChunk.ReadInt32()];
                return output;
            }
        }

        public  class PortholeEntry
        {
            public string Value0 { get; set; }
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string Value3 { get; set; }
            public float Value4 { get; set; }

            public static PortholeEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new PortholeEntry();
                output.Value0 = stringTable[byteChunk.ReadInt32()];
                output.Value1 = stringTable[byteChunk.ReadInt32()];
                output.Value2 = stringTable[byteChunk.ReadInt32()];
                output.Value3 = stringTable[byteChunk.ReadInt32()];
                output.Value4 = byteChunk.ReadSingle();
                return output;
            }
        }

        public class ActionEntry
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public string Type { get; set; }
            public float BlendTime { get; set; }
            public string ActionType { get; set; }
            public int ActionId{ get; set; }
            public bool Unknown { get; set; }

            public static ActionEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new ActionEntry();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];

                output.Type = stringTable[byteChunk.ReadInt32()];
                output.BlendTime = byteChunk.ReadSingle();

                output.ActionType = stringTable[byteChunk.ReadInt32()];
                output.ActionId = byteChunk.ReadInt32();
                output.Unknown = byteChunk.ReadBool();
                return output;
            }
        }

        public class LocomotionEntry
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public string Type { get; set; }
            public float Weight { get; set; }
            public float ModelScale { get; set; }
            public float DistanceTraveled { get; set; }
            public float DistanceMinTravled { get; set; }

            public static LocomotionEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new LocomotionEntry();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Type = stringTable[byteChunk.ReadInt32()];
                output.Weight = byteChunk.ReadSingle();
                output.ModelScale = byteChunk.ReadSingle();
                output.DistanceTraveled = byteChunk.ReadSingle();
                output.DistanceMinTravled = byteChunk.ReadSingle();
                return output;
            }
        }

        public class MissingType
        {
            public static MissingType FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                throw new Exception("Unkown datatype");
            }
        }

        public class UnknownEntry
        {
            public string Animation { get; set; }
            public string AnimationMeta { get; set; }
            public string SoundMeta { get; set; }
            public string Type { get; set; }
            public float BlendTime { get; set; }
            public float Weight { get; set; }
            public int Value { get; set; }

            public static UnknownEntry FromChunck(ByteChunk byteChunk, string[] stringTable)
            {
                var output = new UnknownEntry();
                output.Animation = stringTable[byteChunk.ReadInt32()];
                output.AnimationMeta = stringTable[byteChunk.ReadInt32()];
                output.SoundMeta = stringTable[byteChunk.ReadInt32()];
                output.Type = stringTable[byteChunk.ReadInt32()];
                output.BlendTime = byteChunk.ReadSingle();
                output.Weight = byteChunk.ReadSingle();
                output.Value = byteChunk.ReadInt32();
                return output;
            }
        }
    }



    public class CampaignAnimationBinLoader
    {
        delegate T CreateEntryDelegate<T>(ByteChunk chung, string[] table);

        public static CampaignAnimationBin LoadStuff(ByteChunk data)
        {
            var outputFile = new CampaignAnimationBin();
            outputFile.Version = data.ReadInt32();

            if (outputFile.Version != 3)
                throw new Exception("Version error +" + outputFile.Version);

            var strTableOffset = data.ReadInt32();
            var strTableChunk = new ByteChunk(data.Buffer, strTableOffset);

            var stringTable = ReadStrTable(strTableChunk);

            var offset0 = data.ReadInt32(); // 1
            var offset1 = data.ReadInt32(); // 0
            var offset2 = data.ReadInt32(); // 1

            if (offset0 != 1 || offset1 != 0 || offset2 != 1)
                throw new Exception("Invalid static values");

            outputFile.Reference = stringTable[0];
            outputFile.SkeletonName = stringTable[1];

            var numStatuses = data.ReadInt32();
            for (int i = 0; i < numStatuses; i++)
            {
                var status = LoadStatus(data, stringTable);
                outputFile.Status.Add(status);
            }

            if (strTableOffset != data.Index)
                throw new Exception("Data left");


            return outputFile;
        }

        static CampaignAnimationBin.StatusItem LoadStatus(ByteChunk data, string[] strTable)
        {
            var statusNameIndex = data.ReadInt32();
            var statusName = strTable[statusNameIndex];

            var currentStatus = new CampaignAnimationBin.StatusItem() { Name = statusName };

            if (statusName == "global") // Special case
            {
                currentStatus.PersitantMetaData = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta.FromChunck).FirstOrDefault();
                
                int poseCount, dockCount = 0;
                if (currentStatus.PersitantMetaData != null)
                {
                    poseCount = currentStatus.PersitantMetaData.NumPoses;
                    dockCount = currentStatus.PersitantMetaData.NumDocks;
                }
                else
                {
                    poseCount = data.ReadInt32();
                }

                currentStatus.Poses = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta_Pose.FromChunck, null, poseCount);
                    
                if (currentStatus.PersitantMetaData == null)
                    dockCount = data.ReadInt32();

                currentStatus.Docks = LoadSlots(data, strTable, CampaignAnimationBin.PersistentMeta_Dock.FromChunck, "status_", dockCount);
            }
            else
            {
                currentStatus.Idle = LoadSlots(data, strTable, CampaignAnimationBin.AnimationEntry.FromChunck);
                currentStatus.Porthole = LoadSlots(data, strTable, CampaignAnimationBin.PortholeEntry.FromChunck);     // What is this?
                currentStatus.Selection = LoadSlots(data, strTable, CampaignAnimationBin.AnimationEntry.FromChunck);
                currentStatus.Unk0 = LoadSlots(data, strTable, CampaignAnimationBin.MissingType.FromChunck);                           // CampaginBin_Unknown - What is this?
                currentStatus.Action = LoadSlots(data, strTable, CampaignAnimationBin.ActionEntry.FromChunck);
                currentStatus.Unk1 = LoadSlots(data, strTable, CampaignAnimationBin.MissingType.FromChunck); // Always zero
                currentStatus.Unkown = LoadSlots(data, strTable, CampaignAnimationBin.UnknownEntry.FromChunck); // What is this?
                currentStatus.Unk3 = LoadSlots(data, strTable, CampaignAnimationBin.MissingType.FromChunck);
                currentStatus.Locomotion = LoadSlots(data, strTable, CampaignAnimationBin.LocomotionEntry.FromChunck);
            }

            return currentStatus;
        }


        static List<T> LoadSlots<T>(ByteChunk data, string[] stringTable, CreateEntryDelegate<T> createEntryDelegate, string peakExitCondition = null, int numItems = -1)
        {
            if(numItems == -1)
                numItems = data.ReadInt32();
            var output = new List<T>();
            for (int i = 0; i < numItems; i++)
            {
                // peak
                if (peakExitCondition != null)
                {
                    var peakId = data.PeakUint32();
                    if (peakId >= 0 && peakId < stringTable.Length)
                    {
                        var peakStr = stringTable[peakId];
                        if (peakStr.Contains(peakExitCondition))
                            return output;
                    }
                }

                var newItem = createEntryDelegate(data, stringTable);
                output.Add(newItem);
            }
            if (output.Count == 0)
                return null;

            return output;
        }

        static string[] ReadStrTable(ByteChunk data)
        {
            var stringTable = new List<string>();
            var numTableEntires = data.ReadInt32();
            for (int i = 0; i < numTableEntires; i++)
                stringTable.Add(data.ReadString());
            return stringTable.ToArray();
        }
    }

    public static class AnimationCampaignBinHelper
    {
        public static void BatchProcess(List<PackFile> fileList)
        {
            int counter = 0;
            var failed = new List<string>();
            var output = new List<CampaignAnimationBin>();
            foreach (var f in fileList)
            {
                try
                {
                    var chunkf = f.DataSource.ReadDataAsChunk();
                    var bin = CampaignAnimationBinLoader.LoadStuff(chunkf);
                    output.Add(bin);
                }
                catch (Exception e)
                {
                    failed.Add(f.Name + " " + e.Message);
                }
                counter++;
            }

            var x0 = failed.Where(x => x.Contains("missing schema")).ToList();      // 52
            var x1 = failed.Where(x => x.Contains("Index was outside")).ToList();
            var x2 = failed.Where(x => x.Contains("Metadata error")).ToList();
            var x3 = failed.Where(x => x.Contains("Sequence contains no elements")).ToList();
            var x4 = failed.Where(x => x.Contains("Version error")).ToList();   // 1
            var x5 = failed.Where(x => x.Contains("Unkown datatype")).ToList();   // 1
            
            var eTotal = x0.Count + x1.Count + x2.Count + x3.Count + x4.Count + x5.Count;     // 53 
            return;
        }

        /*public static SimpleSchema CreateSchema()
        {
            var output = new SimpleSchema();

           //var persistMeta = SimpleSchemaObject.Create("CampaginBin_PersistentMeta", 3)
           //                .AddItem("Animation", DbTypesEnum.StringLookup)
           //                .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
           //                .AddItem("AnimationSound", DbTypesEnum.StringLookup)
           //                .AddItem("TypeStr", DbTypesEnum.StringLookup)
           //                .AddItem("BlendTime", DbTypesEnum.Single)
           //                .AddItem("NumPoses", DbTypesEnum.Integer)
           //                 .AddItem("NumDocks", DbTypesEnum.Integer);
           //
           //var persistMeta_pose = SimpleSchemaObject.Create("CampaginBin_PersistentMeta_Pose", 3)
           //              .AddItem("Animation", DbTypesEnum.StringLookup)
           //              .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
           //              .AddItem("AnimationSound", DbTypesEnum.StringLookup)
           //              .AddItem("Weight", DbTypesEnum.Single)
           //              .AddItem("BlendTime", DbTypesEnum.Single)
           //              .AddItem("PoseId", DbTypesEnum.Integer);
           //

            var persistMeta_dock = SimpleSchemaObject.Create("CampaginBin_PersistentMeta_Dock", 3)
                          .AddItem("Animation", DbTypesEnum.StringLookup)
                          .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                          .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                          .AddItem("Weight", DbTypesEnum.Single)
                          .AddItem("BlendTime", DbTypesEnum.Single)
                          .AddItem("Dock", DbTypesEnum.StringLookup);
            

            //var animation = SimpleSchemaObject.Create("CampaginBin_Animation", 3)
            //                .AddItem("Animation", DbTypesEnum.StringLookup)
            //                .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
            //                .AddItem("AnimationSound", DbTypesEnum.StringLookup)
            //                .AddItem("TypeStr", DbTypesEnum.StringLookup)
            //                .AddItem("Weight", DbTypesEnum.Single)
            //                .AddItem("BlendTime", DbTypesEnum.Single);

            var locomotion = SimpleSchemaObject.Create("CampaginBin_Locomotion", 3)
                           .AddItem("Animation", DbTypesEnum.StringLookup)
                           .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                           .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                           .AddItem("TypeStr", DbTypesEnum.StringLookup)
                           .AddItem("Weight", DbTypesEnum.Single)
                           .AddItem("ModelScale", DbTypesEnum.Single)
                           .AddItem("DistanceTraveled", DbTypesEnum.Single)
                           .AddItem("DistanceMinTraveled", DbTypesEnum.Single);

            var action = SimpleSchemaObject.Create("CampaginBin_Action", 3)
                           .AddItem("Animation", DbTypesEnum.StringLookup)
                           .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                           .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                           .AddItem("TypeStr", DbTypesEnum.StringLookup)
                           .AddItem("BlendTime", DbTypesEnum.Single)
                           .AddItem("Action", DbTypesEnum.StringLookup)
                           .AddItem("ActionId", DbTypesEnum.Integer)
                           .AddItem("UnkownBool", DbTypesEnum.Boolean);

            var porthole = SimpleSchemaObject.Create("CampaginBin_Porthole", 3)
                                      .AddItem("Animation", DbTypesEnum.StringLookup)
                                      .AddItem("Animation", DbTypesEnum.StringLookup)
                                      .AddItem("Animation", DbTypesEnum.StringLookup)
                                      .AddItem("Animation", DbTypesEnum.StringLookup)
                                      .AddItem("Animation", DbTypesEnum.Single);


            var unknown = SimpleSchemaObject.Create("CampaginBin_Unknown", 3)
                            .AddItem("Animation", DbTypesEnum.StringLookup)
                            .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                            .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                            .AddItem("TypeStr", DbTypesEnum.StringLookup)
                            .AddItem("Weight", DbTypesEnum.Single)
                            .AddItem("BlendTime", DbTypesEnum.Integer)
                            .AddItem("Skeleton", DbTypesEnum.StringLookup);


            var unknown2 = SimpleSchemaObject.Create("CampaginBin_Unknown2", 3)
                            .AddItem("Animation", DbTypesEnum.StringLookup)
                            .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                            .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                            .AddItem("TypeStr", DbTypesEnum.StringLookup)
                            .AddItem("Weight", DbTypesEnum.Single)
                            .AddItem("BlendTime", DbTypesEnum.Integer)
                            .AddItem("Skeleton", DbTypesEnum.StringLookup);


            output.ObjectDefinitions.AddRange(new SimpleSchemaObject[] { persistMeta, animation, locomotion, action, persistMeta_pose, persistMeta_dock, porthole, unknown, unknown2 });
            return output;
        }*/
    }


}
