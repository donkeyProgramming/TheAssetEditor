using Shared.Core.ByteParsing;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;
using Shared.GameFormats.AnimationPack.AnimPackFileTypes;

namespace Shared.GameFormats.AnimationPack
{
    public class AnimationEntryMetaData
    {
        public string Name { get; set; }
        public int StartOffset { get; set; }
        public int Size { get; set; }

        public AnimationEntryMetaData()
        { }

        public AnimationEntryMetaData(ByteChunk data)
        {
            Name = data.ReadString();
            Size = data.ReadInt32();
            StartOffset = data.Index;
        }

        public byte[] ToByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.String.WriteCaString(Name));
            memStream.Write(ByteParsers.Int32.EncodeValue(Size, out _));
            return memStream.ToArray();
        }
    }



    public static class AnimationPackSerializer
    {
        static IAnimFileSerializer DeterminePossibleSerializers(string fullPath)
        {
            if (fullPath.Contains("animations/database/battle/bin/", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fullPath.Contains("matched_combat", StringComparison.InvariantCultureIgnoreCase))
                    return new MatchedAnimFileSerializer();
                else if (fullPath.Contains(".bin", StringComparison.InvariantCultureIgnoreCase))
                    return new AnimationSetSerializer_Wh3();
                else
                    return new UnknownAnimFileSerializer();
            }
            else if (fullPath.Contains("animations/database/", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fullPath.Contains("matched", StringComparison.InvariantCultureIgnoreCase))
                    return new UnknownAnimFileSerializer();
                else if (fullPath.Contains("trigger_database", StringComparison.InvariantCultureIgnoreCase))
                    return new UnknownAnimFileSerializer();
            }

            else
            {
                if (fullPath.Contains(".frg", StringComparison.InvariantCultureIgnoreCase))
                    return new AnimationSetFileSerializer();
                else if (fullPath.Contains("matched_combat", StringComparison.InvariantCultureIgnoreCase))
                    return new MatchedAnimFileSerializer();
                else if (fullPath.Contains(".bin", StringComparison.InvariantCultureIgnoreCase))
                    return new AnimationDbFileSerializer();
            }

            return new UnknownAnimFileSerializer();
        }

        public static AnimationPackFile Load(PackFile pf, IPackFileService pfs, GameTypeEnum preferedGame = GameTypeEnum.Unknown)
        {
            var output = new AnimationPackFile(pfs.GetFullPath(pf));

            var dataChunk = pf.DataSource.ReadDataAsChunk();
            var files = FindAllSubFiles(dataChunk);

            foreach (var file in files)
            {
                var fileLoader = DeterminePossibleSerializers(file.Name);
                var loadedFile = LoadFile(fileLoader, file, dataChunk, preferedGame);
                output.AddFile(loadedFile);
            }

            return output;
        }

        public static byte[] ConvertToBytes(AnimationPackFile animPack)
        {
            using var memStream = new MemoryStream();

            var totalFileCount = animPack.Files.Count();
            memStream.Write(ByteParsers.Int32.EncodeValue(totalFileCount, out _));

            foreach (var item in animPack.Files)
            {
                var itemByteArray = item.ToByteArray();
                var file = new AnimationEntryMetaData()
                {
                    Name = item.FileName,
                    Size = itemByteArray.Length
                };
                memStream.Write(file.ToByteArray());
                memStream.Write(itemByteArray);
            }

            return memStream.ToArray();
        }

        static IAnimationPackFile LoadFile(IAnimFileSerializer serializer, AnimationEntryMetaData animationInfoDataFile, ByteChunk data, GameTypeEnum preferedGame)
        {
            try
            {
                return serializer.Load(animationInfoDataFile, data, preferedGame);
            }
            catch
            {
                return new UnknownAnimFile(animationInfoDataFile.Name, data.Buffer);
            }
        }

        static List<AnimationEntryMetaData> FindAllSubFiles(ByteChunk data)
        {
            var toalFileCount = data.ReadInt32();
            var fileList = new List<AnimationEntryMetaData>(toalFileCount);
            for (var i = 0; i < toalFileCount; i++)
            {
                var file = new AnimationEntryMetaData(data);
                fileList.Add(file);
                data.Index += file.Size;
            }
            return fileList;
        }
    }

    public interface IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData animFileInfo, ByteChunk data, GameTypeEnum preferedGame);
    }

    public class UnknownAnimFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData info, ByteChunk data, GameTypeEnum preferedGam) => new UnknownAnimFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class AnimationSetFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData info, ByteChunk data, GameTypeEnum preferedGame) => new AnimationFragmentFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size), preferedGame);
    }

    public class AnimationDbFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData info, ByteChunk data, GameTypeEnum preferedGame) => new AnimationBin(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class MatchedAnimFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData info, ByteChunk data, GameTypeEnum preferedGame) => new MatchedAnimFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class AnimationSetSerializer_Wh3 : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationEntryMetaData info, ByteChunk data, GameTypeEnum preferedGame) => new AnimPackFileTypes.Wh3.AnimationBinWh3(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public interface IMatchedCombatBin : IAnimationPackFile
    { }




}
