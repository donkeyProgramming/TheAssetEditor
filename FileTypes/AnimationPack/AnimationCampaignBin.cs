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
    public class AnimationCampaignBin
    {
        string[] _stringTable;

        public int Version { get; set; }

        public Dictionary<string, StatusInformation> Content = new Dictionary<string, StatusInformation>();

        public class StatusInformation
        {
            public string Name { get; set; }
            public SimpleSchemaObject PersistentMeta { get; set; }
            public List<SimpleSchemaObject> Poses { get; set; }
            public List<SimpleSchemaObject> Docks { get; set; }

            public List<SimpleSchemaObject> Idle { get; set; } 
            public List<SimpleSchemaObject> unk0 { get; set; }
            public List<SimpleSchemaObject> Selection { get; set; }
            public List<SimpleSchemaObject> ukn1 { get; set; }
            public List<SimpleSchemaObject> Actions { get; set; }
            public List<SimpleSchemaObject> ukn3  { get; set; }
            public List<SimpleSchemaObject> ukn4  { get; set; }
            public List<SimpleSchemaObject> ukn5 { get; set; }
            public List<SimpleSchemaObject> Locomotion { get; set; }
        }

        public AnimationCampaignBin(ByteChunk data)
        {
            var schemas = AnimationCampaignBinHelper.CreateSchema();

            Version = data.ReadInt32();
            var strTableOffset = data.ReadInt32();
            var strTableChunk = new ByteChunk(data.Buffer, strTableOffset);

            _stringTable = ReadStrTable(strTableChunk);

            var offset0 = data.ReadInt32(); // 1
            var offset1 = data.ReadInt32(); // 0
            var offset2 = data.ReadInt32(); // 1

            if (offset0 != 1 || offset1 != 0 || offset2 != 1)
                throw new Exception("Invalid static values");

            var numStatuses = data.ReadInt32();

            if (Version != 3)
                throw new Exception("Version error +" + Version);

            for (int i = 0; i < numStatuses; i++)
                LoadStatus(data, _stringTable, schemas, Version);

            if (strTableOffset != data.Index)
                throw new Exception("Data left");
        }


        void LoadStatus(ByteChunk data, string[] strTable, SimpleSchema schemas, int version)
        {
            var statusNameIndex = data.ReadInt32();
            var statusName = strTable[statusNameIndex];

            var currentStatus = new StatusInformation() { Name = statusName };
            Content.Add(statusName, currentStatus);

            if (statusName == "global") // Special case
            {
                currentStatus.PersistentMeta = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_PersistentMeta", version)).FirstOrDefault();
                
                int poseCount, dockCount = 0;
                if (currentStatus.PersistentMeta != null)
                {
                    poseCount = int.Parse(currentStatus.PersistentMeta.Fields[5].Value);
                    dockCount = int.Parse(currentStatus.PersistentMeta.Fields[6].Value);
                }
                else
                {
                    poseCount = data.ReadInt32();
                }

                currentStatus.Poses = LoadSlots(data, poseCount, strTable, schemas.GetObjectDefinition("CampaginBin_PersistentMeta_Pose", version));
                    
                if (currentStatus.PersistentMeta == null)
                    dockCount = data.ReadInt32();

                currentStatus.Docks = LoadSlots(data, dockCount, strTable, schemas.GetObjectDefinition("CampaginBin_PersistentMeta_Dock", version), "status_");
            }
            else
            {
                currentStatus.Idle = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Animation", version));
                currentStatus.unk0 = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Porthole", version));     // What is this?
                currentStatus.Selection = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Animation", version));
                currentStatus.ukn1 = LoadSlots(data, strTable, schemas.GetObjectDefinition("", version));                           // CampaginBin_Unknown - What is this?
                currentStatus.Actions = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Action", version));
                currentStatus.ukn3 = LoadSlots(data, strTable, schemas.GetObjectDefinition("", version)); // Always zero
                currentStatus.ukn4 = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Unknown2", version)); // What is this?
                currentStatus.ukn5 = LoadSlots(data, strTable, schemas.GetObjectDefinition("", version));
                currentStatus.Locomotion = LoadSlots(data, strTable, schemas.GetObjectDefinition("CampaginBin_Locomotion", version));
            }
        }

        List<SimpleSchemaObject> LoadSlots(ByteChunk data, string[] stringTable, SimpleSchemaObject schemaObject)
        {
            var count = data.ReadInt32();
            return LoadSlots(data, count, stringTable, schemaObject);
        }

        List<SimpleSchemaObject> LoadSlots(ByteChunk data, int numItems, string[] stringTable, SimpleSchemaObject schemaObject, string peakExitCondition = "")
        {
            if (numItems != 0 && schemaObject == null)
                throw new Exception("missing schema " + numItems);

            var output = new List<SimpleSchemaObject>();

            if (numItems == 0)
                return output;

            for (int i = 0; i < numItems; i++)
            {
                var peakU = data.PeakUnknown();

                if (!string.IsNullOrEmpty(peakExitCondition))
                {
                    var d = data.PeakUint32();
                    if (d >= 0 && d < stringTable.Length)
                    {
                        var peakStr = stringTable[d];
                        if (peakStr.Contains(peakExitCondition))
                            return output;
                    }
                }

                var instance = schemaObject.Decode(data, stringTable);
                output.Add(instance);
            }
            return output;
        }

        string[] ReadStrTable(ByteChunk data)
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
            List<string> failed = new List<string>();
            foreach (var f in fileList)
            {
                try
                {
                    var chunkf = f.DataSource.ReadDataAsChunk();
                    var r = new AnimationCampaignBin(chunkf);
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
            var eTotal = x0.Count + x1.Count + x2.Count + x3.Count + x4.Count;     // 53 
            return;
        }

        public static SimpleSchema CreateSchema()
        {
            var output = new SimpleSchema();

            var persistMeta = SimpleSchemaObject.Create("CampaginBin_PersistentMeta", 3)
                            .AddItem("Animation", DbTypesEnum.StringLookup)
                            .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                            .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                            .AddItem("TypeStr", DbTypesEnum.StringLookup)
                            .AddItem("BlendTime", DbTypesEnum.Single)
                            .AddItem("NumPoses", DbTypesEnum.Integer)
                             .AddItem("NumDocks", DbTypesEnum.Integer);

            var persistMeta_pose = SimpleSchemaObject.Create("CampaginBin_PersistentMeta_Pose", 3)
                          .AddItem("Animation", DbTypesEnum.StringLookup)
                          .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                          .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                          .AddItem("Weight", DbTypesEnum.Single)
                          .AddItem("BlendTime", DbTypesEnum.Single)
                          .AddItem("PoseId", DbTypesEnum.Integer);


            var persistMeta_dock = SimpleSchemaObject.Create("CampaginBin_PersistentMeta_Dock", 3)
                          .AddItem("Animation", DbTypesEnum.StringLookup)
                          .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                          .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                          .AddItem("Weight", DbTypesEnum.Single)
                          .AddItem("BlendTime", DbTypesEnum.Single)
                          .AddItem("Dock", DbTypesEnum.StringLookup);


            var animation = SimpleSchemaObject.Create("CampaginBin_Animation", 3)
                            .AddItem("Animation", DbTypesEnum.StringLookup)
                            .AddItem("AnimationMeta", DbTypesEnum.StringLookup)
                            .AddItem("AnimationSound", DbTypesEnum.StringLookup)
                            .AddItem("TypeStr", DbTypesEnum.StringLookup)
                            .AddItem("Weight", DbTypesEnum.Single)
                            .AddItem("BlendTime", DbTypesEnum.Single);

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
        }
    }


}
