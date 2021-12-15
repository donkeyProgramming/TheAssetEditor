using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonControls.FileTypes.AnimationPack
{
    public class AnimationInfoDataFile
    {
        public string Name { get; set; }
        public int StartOffset { get; set; }
        public int Size { get; set; }

        public AnimationInfoDataFile()
        { }

        public AnimationInfoDataFile(ByteChunk data)
        {
            Name = data.ReadString();
            Size = data.ReadInt32();
            StartOffset = data.Index;
        }

        public byte[] ToByteArray()
        {
            using MemoryStream memStream = new MemoryStream();
            memStream.Write(ByteParsers.String.WriteCaString(Name));
            memStream.Write(ByteParsers.Int32.EncodeValue(Size, out _));
            return memStream.ToArray();
        }
    }

    

    public static class AnimationPackSerializer
    {
        static IAnimFileSerializer DeterminePossibleSerializers(string fullPath)
        {
            // 3k
            if (fullPath.Contains("animations/database/", StringComparison.InvariantCultureIgnoreCase))
            {
                if (fullPath.Contains("matched", StringComparison.InvariantCultureIgnoreCase))
                    return new UnknownAnimFileSerializer();
                else if (fullPath.Contains("trigger_database", StringComparison.InvariantCultureIgnoreCase))
                    return new UnknownAnimFileSerializer();
                else
                    return new AnimationSetSerializer_3K();
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

            return null;

            //throw new Exception();
        }

        public static AnimationPackFile Load(PackFile pf, PackFileService pfs)
        {
            var output = new AnimationPackFile();
            output.FileName = pfs.GetFullPath(pf);

            var dataChunk = pf.DataSource.ReadDataAsChunk();
            var files = FindAllSubFiles(dataChunk);

            foreach (var file in files)
            {
                bool isLoaded = false;
                var fileLoader = DeterminePossibleSerializers(file.Name);
                if (fileLoader != null)
                {
                    var loadedFile = LoadFile(fileLoader, file, dataChunk);
                    if (loadedFile != null)
                    {
                        output.AddFile(loadedFile);
                        isLoaded = true;
                    }
                }

               if (isLoaded == false)
               {
                   var unkownLoader = new UnknownAnimFileSerializer();
                   var unkownFile = LoadFile(unkownLoader, file, dataChunk);
                   output.AddFile(unkownFile);
               }
            }

            return output;
        }

        public static byte[] ConvertToBytes(AnimationPackFile animPack)
        {
            using MemoryStream memStream = new MemoryStream();

            int totalFileCount = animPack.Files.Count();
            memStream.Write(ByteParsers.Int32.EncodeValue(totalFileCount, out _));

            foreach (var item in animPack.Files)
            {
                var itemByteArray = item.ToByteArray();
                var file = new AnimationInfoDataFile()
                {
                    Name = item.FileName,
                    Size = itemByteArray.Length
                };
                memStream.Write(file.ToByteArray());
                memStream.Write(itemByteArray);
            }

            return memStream.ToArray();
        }

        static IAnimationPackFile LoadFile(IAnimFileSerializer serializer, AnimationInfoDataFile animationInfoDataFile, ByteChunk data)
        {
            try
            {
                return serializer.Load(animationInfoDataFile, data);
            }
            catch(Exception e)
            {
                return null;
            }
        }

        static List<AnimationInfoDataFile> FindAllSubFiles(ByteChunk data)
        {
            var toalFileCount = data.ReadInt32();
            var fileList = new List<AnimationInfoDataFile>(toalFileCount);
            for (int i = 0; i < toalFileCount; i++)
            {
                var file = new AnimationInfoDataFile(data);
                fileList.Add(file);
                data.Index += file.Size;
            }
            return fileList;
        }
    }

    public interface IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, ByteChunk data);
    }

    public class UnknownAnimFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile info, ByteChunk data) => new UnknownAnimFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class AnimationSetFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile info, ByteChunk data) => new AnimationSetFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class AnimationDbFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile info, ByteChunk data) => new AnimationDbFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }

    public class MatchedAnimFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile info, ByteChunk data) => new MatchedAnimFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }


    public class AnimationSetSerializer_3K : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile info, ByteChunk data) => new AnimationSet3kFile(info.Name, data.GetBytesFromBuffer(info.StartOffset, info.Size));
    }


    //
    //
    //public class AnimMatchSerializer : IAnimFileSerializer
    //{
    //    public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, byte[] data)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
    //
    //public class AnimMatchSerializer_3k : IAnimFileSerializer
    //{
    //    public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, byte[] data)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
    //
    //public class AnimTriggerDb_3k : IAnimFileSerializer
    //{
    //    public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, byte[] data)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}



    // ----------------------





    public interface IAnimationBin : IAnimationPackFile
    { }

    public interface IAnimationFragment : IAnimationPackFile
    { }

    public interface IMatchedCombatBin : IAnimationPackFile
    { }

    public interface IAbilityBin_3k : IAnimationPackFile
    {
    }

  
}
