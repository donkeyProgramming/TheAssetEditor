// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommonControls.Services;

namespace CommonControls.FileTypes.AnimationPack
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

    public class BaseAnimationSlotHelper
    {
        public List<AnimationSlotType> Values { get; private set; }

        public BaseAnimationSlotHelper(GameTypeEnum game)
        {
            switch (game)
            {
                case GameTypeEnum.Warhammer2:
                    Load("CommonControls.Resources.AnimationSlots.Warhammer2AnimationSlots.txt");
                    break;

                case GameTypeEnum.Warhammer3:
                    Load("CommonControls.Resources.AnimationSlots.Warhammer3AnimationSlots_dlc24.txt");
                    break;

                case GameTypeEnum.Troy:
                    Load("CommonControls.Resources.AnimationSlots.TroyAnimationSlots.txt");
                    break;

                case GameTypeEnum.ThreeKingdoms:
                    Load("CommonControls.Resources.AnimationSlots.3kAnimationSlots.txt");
                    break;

                default:
                    Load("CommonControls.Resources.AnimationSlots.Warhammer2AnimationSlots.txt");
                    break;
            }
        }

        public AnimationSlotType TryGetFromId(int id)
        {
            if (id >= 0 && id < Values.Count)
                return Values[id];
            return null;
        }

        public AnimationSlotType GetfromValue(string value)
        {
            var upperStr = value.ToUpper();
            return Values.FirstOrDefault(x => x.Value == upperStr);
        }

        public AnimationSlotType GetMatchingRiderAnimation(string value)
        {
            var riderAnim = "RIDER_" + value;
            return Values.FirstOrDefault(x => x.Value == riderAnim);
        }


        void Load(string resourcePath)
        {
            Values = new List<AnimationSlotType>();
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            using (var reader = new StreamReader(stream))
            {
                string[] result = reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < result.Length; i++)
                    Values.Add(new AnimationSlotType(i, result[i].Trim()));
            }
        }

        public void ExportAnimationDebugList(PackFileService pfs, string outputName)
        {
            var data = new Dictionary<string, List<string>>();
            var indexList = new Dictionary<string, int>();
            foreach (var slot in Values)
                data[slot.Value] = new List<string>();

            var animPacks = pfs.GetAllAnimPacks();
            foreach (var animPack in animPacks)
            {
                var animPackFile = AnimationPackSerializer.Load(animPack, pfs);
                var fragments = animPackFile.GetGenericAnimationSets();

                foreach (var fragment in fragments)
                {
                    foreach (var entry in fragment.Entries)
                    {
                        if (data.ContainsKey(entry.SlotName) == false)
                            data[entry.SlotName] = new List<string>();

                        var fileName = Path.GetFileNameWithoutExtension(entry.AnimationFile);
                        data[entry.SlotName].Add(fileName);
                        indexList[entry.SlotName] = entry.SlotIndex;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"sep=|");
            sb.AppendLine($"SlotName|SlotIndex|Files");
            foreach (var row in data)
            {
                var files = string.Join(", ", row.Value.Distinct());
                var slotId = indexList.ContainsKey(row.Key) ? indexList[row.Key].ToString() : "";
                sb.AppendLine($"{row.Key.PadRight(45, ' ')}\t|{slotId.PadRight(5)}| {files}");
            }
            File.WriteAllText($"C:\\temp\\{outputName}.csv", sb.ToString());
        }
    }

    public static class AnimationSlotTypeHelperWh3
    {
        static BaseAnimationSlotHelper Instance;
        public static List<AnimationSlotType> Values { get { Create(); return Instance.Values; } }

        static public AnimationSlotType GetFromId(int id)
        {
            Create();
            return Values[id];
        }

        static public AnimationSlotType GetfromValue(string value)
        {
            Create();
            var upperStr = value.ToUpper();
            return Values.FirstOrDefault(x => x.Value == upperStr);
        }

        static public AnimationSlotType GetMatchingRiderAnimation(string value)
        {
            var riderAnim = "RIDER_" + value;
            return Values.FirstOrDefault(x => x.Value == riderAnim);
        }

        static void Create()
        {
            if (Instance == null)
                Instance = new BaseAnimationSlotHelper(GameTypeEnum.Warhammer3);
        }

        public static BaseAnimationSlotHelper GetInstance()
        {
            Create();
            return Instance;
        }
    }


    public static class AnimationSlotTypeHelper3k
    {
        public static BaseAnimationSlotHelper Instance;
        public static List<AnimationSlotType> Values { get { Create(); return Instance.Values; } }

        static public AnimationSlotType GetFromId(int id)
        {
            Create();
            return Values[id];
        }

        static public AnimationSlotType GetfromValue(string value)
        {
            Create();
            var upperStr = value.ToUpper();
            return Values.FirstOrDefault(x => x.Value == upperStr);
        }

        static public AnimationSlotType GetMatchingRiderAnimation(string value)
        {
            var riderAnim = "RIDER_" + value;
            return Values.FirstOrDefault(x => x.Value == riderAnim);
        }

        static void Create()
        {
            if (Instance == null)
                Instance = new BaseAnimationSlotHelper(GameTypeEnum.ThreeKingdoms);
        }

        public static BaseAnimationSlotHelper GetInstance()
        {
            Create();
            return Instance;
        }
    }
    public static class AnimationSlotTypeHelperTroy
    {
        public static BaseAnimationSlotHelper Instance;
        public static List<AnimationSlotType> Values { get { Create(); return Instance.Values; } }

        static public AnimationSlotType GetFromId(int id)
        {
            Create();
            return Values[id];
        }

        static public AnimationSlotType GetfromValue(string value)
        {
            Create();
            var upperStr = value.ToUpper();
            return Values.FirstOrDefault(x => x.Value == upperStr);
        }

        static public AnimationSlotType GetMatchingRiderAnimation(string value)
        {
            var riderAnim = "RIDER_" + value;
            return Values.FirstOrDefault(x => x.Value == riderAnim);
        }

        static void Create()
        {
            if (Instance == null)
                Instance = new BaseAnimationSlotHelper(GameTypeEnum.Troy);
        }

        public static BaseAnimationSlotHelper GetInstance()
        {
            Create();
            return Instance;
        }
    }

    public static class DefaultAnimationSlotTypeHelper
    {
        static BaseAnimationSlotHelper Instance;
        public static List<AnimationSlotType> Values { get { Create(); return Instance.Values; } }

        static public AnimationSlotType GetFromId(int id)
        {
            Create();
            return Values[id];
        }

        static public AnimationSlotType GetfromValue(string value)
        {
            Create();
            var upperStr = value.ToUpper();
            return Values.FirstOrDefault(x => x.Value == upperStr);
        }

        static public AnimationSlotType GetMatchingRiderAnimation(string value)
        {
            var riderAnim = "RIDER_" + value;
            return Values.FirstOrDefault(x => x.Value == riderAnim);
        }

        static void Create()
        {
            if (Instance == null)
                Instance = new BaseAnimationSlotHelper(GameTypeEnum.Warhammer2);
        }

        public static BaseAnimationSlotHelper GetInstance()
        {
            Create();
            return Instance;
        }
    }
}

