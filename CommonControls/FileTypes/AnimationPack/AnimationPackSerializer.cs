using CommonControls.FileTypes.AnimationPack.AnimPackFileTypes;
using CommonControls.FileTypes.PackFiles.Models;
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

    public class AnimationPackFile
    {
        public string FileName { get; set; }
        List<IAnimationPackFile> _files { get; set; } = new List<IAnimationPackFile>();

        public IEnumerable<IAnimationPackFile> Files { get => _files; }

        public void AddFile(IAnimationPackFile file)
        {
            file.Parent = this;
            _files.Add(file);
        }

        public List<AnimationSetFile> GetAnimationSets(string skeletonName = null)
        {
            var sets = _files.Where(x => x is AnimationSetFile).Cast<AnimationSetFile>();
            if(skeletonName != null)
                sets = sets.Where(x => x.Skeletons.Values.Contains(skeletonName));

            return sets.ToList();
        }
    }

    public static class AnimationPackSerializer
    {
        public static AnimationPackFile Load(PackFile fp)
        {
            var output = new AnimationPackFile();

            var fileLoaders = new Dictionary<string, List<IAnimFileSerializer>>();
            fileLoaders.Add(".frg", new List<IAnimFileSerializer>() { new AnimationSetSerializer()/*, new AnimationSetSerializer_3K()*/});
            fileLoaders.Add(".bin", new List<IAnimFileSerializer>() { new AnimDbSerializer() });//, new AnimMatchSerializer(), new AnimMatchSerializer_3k(), new AnimTriggerDb_3k() });

            var dataChunk = fp.DataSource.ReadDataAsChunk();
            var files = FindAllSubFiles(dataChunk);

            foreach (var file in files)
            {
                var extention = Path.GetExtension(file.Name);

                bool isLoaded = false;
                if (fileLoaders.ContainsKey(extention))
                {
                    foreach (var fileLoader in fileLoaders[extention])
                    {
                        var loadedFile = LoadFile(fileLoader, file, dataChunk);
                        if (loadedFile != null)
                        {
                            output.AddFile(loadedFile);
                            isLoaded = true;
                            continue;
                        }
                    }
                }

                //if (isLoaded == false)
                //{
                //    var unkownLoader = new UnknownAnimFileSerializer();
                //    var loadedFile = LoadFile(unkownLoader, file, dataChunk);
                //    output.Files.Add(loadedFile);
                //}
            }


            // Set parent pack to all files
            // Assert all files belong to same version of the game 

            return output;
        }

        public static byte[] ConvertToBytes(AnimationPackFile animPack)
        {
            //if (HasUnknownElements)
            //    throw new Exception("Can not save animation pack with unkown elements");

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

    public class AnimationSetSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, ByteChunk data)
        {
            var buffer = data.GetBytesFromBuffer(animFileInfo.StartOffset, animFileInfo.Size);
            return new AnimationSetFile(animFileInfo.Name, buffer);
        }
    }

    public class AnimDbSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, ByteChunk data)
        {
            var buffer = data.GetBytesFromBuffer(animFileInfo.StartOffset, animFileInfo.Size);
            return new AnimationDbFile(animFileInfo.Name, buffer);
        }
    }

    public class UnknownAnimFileSerializer : IAnimFileSerializer
    {
        public IAnimationPackFile Load(AnimationInfoDataFile animFileInfo, ByteChunk data)
        {
            throw new System.NotImplementedException();
        }
    }



    //public class AnimationSetSerializer_3K : IAnimFileSerializer
    //{ }
    //

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
