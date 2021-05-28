using Common;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.AnimationPack
{
    public class AnimationPackLoader
    {
        class AnimationDataFile
        {
            public string Name { get; set; }
            public int StartOffset { get; set; }
            public int Size { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public List<AnimationTableEntry> AnimationTableEntries { get; set; } = new List<AnimationTableEntry>();
        public List<MatchedAnimationTableEntry> MatchedAnimationTableEntries { get; set; } = new List<MatchedAnimationTableEntry>();
        public List<AnimationFragmentFile> AnimationFragments { get; set; } = new List<AnimationFragmentFile>();


        delegate void ProcessFileDelegate(AnimationDataFile file, ByteChunk data);
        Dictionary<string, ProcessFileDelegate> _processMap = new Dictionary<string, ProcessFileDelegate>();

        //-------------
        // Bin File
        //  Fragments
        //  Matched animations
        //  Animation tables


        static public IEnumerable<AnimationFragmentFile> GetFragmentCollections(PackFile file)
        {
            var d = file.DataSource.ReadData();
            ByteChunk data = new ByteChunk(d);
            var fragmentFiles = FindAllSubFiles(data).Where(x => x.Name.Contains(".frg"));

            var animationFragmentCollections = new List<AnimationFragmentFile>();
            foreach (var fragmentFile in fragmentFiles)
            {
                data.Index = fragmentFile.StartOffset;
                animationFragmentCollections.Add(new AnimationFragmentFile(fragmentFile.Name, data));
            }

            return animationFragmentCollections;
        }

       /* static public AnimationFragmentCollection GetOldFragmentCollection(PackFile file)
        {
            var d = file.DataSource.ReadData();
            ByteChunk data = new ByteChunk(d);
            var str = System.Text.Encoding.Default.GetString(d);
            var lines = str.Split('\n');

            var collection = new AnimationFragmentCollection(file.FullPath);

            int fileVersion = -1;
            string skeletonName = string.Empty;
            var fragmentCollection = new List<AnimationFragmentItem>();
            foreach (var line in lines)
            {
                var wordList = line
                    .Trim()
                    .Split('\t')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (ParseEmptyLine(wordList))
                    continue;

                if (ParseVersionLine(wordList, ref fileVersion))
                    continue;

                if (ParseSkeletonName(wordList, ref skeletonName))
                {
                    collection.Skeletons.Values.Add(skeletonName);
                    continue;
                }

                if (ParseFragment(wordList, collection.AnimationFragments))
                    continue;

                throw new Exception($"Unable to parse line : {line}");
            }

            return collection;
        }*/

 
        static bool ParseEmptyLine(List<string> lineList)
        {
            return lineList.Count() == 0;
        }

        static bool ParseVersionLine(List<string> lineList, ref int version)
        {
            version = 0;
            if (lineList[0].Contains("version"))
            {
                if (lineList[0].Contains("version 2"))
                    version = 2;
                else
                    throw new Exception("Unknown version " + lineList[0]);
                return true;
            }
              
            return false;
        }


        static bool ParseSkeletonName(List<string> lineList, ref string skeletonName)
        {
            if (lineList.Count == 2 && lineList[0] == "skeleton_type")
            {
                skeletonName = lineList[1];
                return true;
            }
            return false;
        }

        static bool ParseFragment(List<string> lineList, IList< Fragment> fragmentList)
        {
            var fragmentItem = new Fragment();

            fragmentItem.Slot= AnimationSlotTypeHelper.GetfromValue(lineList[0]);
            fragmentItem.AnimationFile = GetStrValue(lineList[1]);
            fragmentItem.MetaDataFile = GetStrValue(lineList[2]);
            fragmentItem.SoundMetaDataFile = GetStrValue(lineList[3]);
            if (lineList.Count == 5)
            {
                var other = lineList[4]
                    .Split(',')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                var currentParseInxed =0;
                for (; currentParseInxed < other.Length; currentParseInxed++)
                {
                    if (ParseBlendInTime(other[currentParseInxed], out var blendInTime))
                    {
                        fragmentItem.Blend = blendInTime;
                        continue;
                    }
                
                    if (ParseSelectionWeight(other[currentParseInxed], out var selectionWeight))
                    {
                        fragmentItem.Wight = selectionWeight;
                        continue;
                    }

                    for (int boneIndex = 0; boneIndex < 10; boneIndex++)
                    {
                        if (ParseWeaponBone(other[currentParseInxed], boneIndex, out var boneFlag))
                        {
                            fragmentItem.SetWeaponFlag(boneIndex-1, boneFlag);
                           continue;
                        }
                    }
                }

                if (currentParseInxed != other.Length)
                    throw new Exception("Not everythign is parsed!");
            }

            fragmentList.Add(fragmentItem);
            return true;
        }

        static string GetStrValue(string str)
        {
            var valStr0 = str.Substring(str.IndexOf('=') + 1).Trim();
            var velStr1 = valStr0.Substring(1, valStr0.Length - 2);
            return velStr1;
            //COMBAT_IDLE_1				
            //filename = "animations/battle/humanoid02/2handed_axe/combat_idles/hu2_2ha_combat_idle_01.anim"		
            //metadata = "animations/battle/humanoid02/2handed_axe/combat_idles/hu2_2ha_combat_idle_01.anm.meta"		
            //metadata_sound = "animations/battle/humanoid02/2handed_axe/combat_idles/hu2_2ha_combat_idle_01.snd.meta"	
        }

        static bool ParseBlendInTime(string item, out float value)
        {
            if (item.Contains("blend_in_time"))
            {
                var valStr = item.Substring(item.IndexOf('=') + 1);
                value = float.Parse(valStr);
                return true;
            }
            value = 0;
            return false;
        }

        static bool ParseSelectionWeight(string item, out float value)
        {
            if (item.Contains("selection_weight"))
            {
                var valStr = item.Substring(item.IndexOf('=') + 1);
                value = float.Parse(valStr);
                return true;
            }
            value = 0;
            return false;
        }

        static bool ParseWeaponBone(string item, int boneIndex, out bool onOff)
        {
            onOff = false;
            if (item.Contains("weapon_bone_" + boneIndex))
            {
                var valStr = item.Substring(item.IndexOf('=') + 1).Trim();
                if (valStr == "on")
                    onOff = true;

                return true;
            }
            return false;
        }


        static public IEnumerable<AnimationTableEntry> GetAnimationTables(PackFile file)
        {
            var d = file.DataSource.ReadData();
            ByteChunk data = new ByteChunk(d);
            var animationTableFile = FindAllSubFiles(data).First(x => x.Name.Contains("animation_tables.bin"));

            data.Index = animationTableFile.StartOffset;
            var tableVersion = data.ReadInt32();
            var rowCount = data.ReadInt32();
            var animationTableEntries = new List<AnimationTableEntry>(rowCount);
            for (int i = 0; i < rowCount; i++)
                animationTableEntries.Add(new AnimationTableEntry(data));

            return animationTableEntries;
        }








        public void Load(ByteChunk data)
        {
            data.Reset();
            _processMap["attila_generated.bin"] = ProcessMatchCombatFile;
            _processMap["animation_tables.bin"] = ProcessAnimationTableFile;
            _processMap[".frg"] = ProcessFragmentFile;

            var files = FindAllSubFiles(data);
            foreach (var file in files)
            {
                bool isProcessed = false;
                foreach (var process in _processMap)
                {
                    if (file.Name.Contains(process.Key))
                    {
                        process.Value(file, data);
                        isProcessed = true;
                        break;
                    }
                }
                if(!isProcessed)
                    throw new Exception($"Unknown file - {file.Name}");
            }
        }

        static List<AnimationDataFile> FindAllSubFiles(ByteChunk data)
        {
            var toalFileCount = data.ReadInt32();
            var fileList = new List<AnimationDataFile>(toalFileCount);
            for (int i = 0; i < toalFileCount; i++)
            {
                var file = new AnimationDataFile()
                {
                    Name = data.ReadString(),
                    Size = data.ReadInt32(),
                    StartOffset = data.Index
                };
                fileList.Add(file);
                data.Index += file.Size;
            }
            return fileList;
        }

        void ProcessFragmentFile(AnimationDataFile file, ByteChunk data)
        {
            data.Index = file.StartOffset;
            AnimationFragments.Add(new AnimationFragmentFile(file.Name, data));
        }

        void ProcessMatchCombatFile(AnimationDataFile file, ByteChunk data)
        {
            data.Index = file.StartOffset;
            var tableVersion = data.ReadInt32();
            var rowCount = data.ReadInt32();
            MatchedAnimationTableEntries = new List<MatchedAnimationTableEntry>(rowCount);
            for (int i = 0; i < rowCount; i++)
            {
                var entry = new MatchedAnimationTableEntry(data);
                MatchedAnimationTableEntries.Add(entry);
            }
        }

        void ProcessAnimationTableFile(AnimationDataFile file, ByteChunk data)
        {
            data.Index = file.StartOffset;
            var tableVersion = data.ReadInt32();
            var rowCount = data.ReadInt32();
            AnimationTableEntries = new List<AnimationTableEntry>(rowCount);
            for (int i = 0; i < rowCount; i++)
            {
                var entry = new AnimationTableEntry(data);
                AnimationTableEntries.Add(entry);
            }
        }



        



    }
}
