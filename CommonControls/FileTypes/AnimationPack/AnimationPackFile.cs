using CommonControls.FileTypes.PackFiles.Models;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonControls.FileTypes.AnimationPack
{
    public class AnimationPackFile
    {

        public enum AnimationPackFileType
        {
            Bin,
            Fragment,
            MatchedCombat,
            Unknown
        }

        class AnimationDataFile
        {
            public string Name { get; set; }
            public int StartOffset { get; set; }
            public int Size { get; set; }

            public AnimationDataFile()
            { }

            public AnimationDataFile(ByteChunk data)
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

        public List<AnimationFragment> Fragments { get; set; } = new List<AnimationFragment>();
        public AnimationBin AnimationBin { get; set; }
        public bool HasUnknownElements { get; set; } = false;
        public string FileName { get; set; }

        public AnimationPackFile(PackFile file, string onlyForThisSkeleton = null)
        {
            var data = file.DataSource.ReadDataAsChunk();
            FileName = file.Name;

            var files = FindAllSubFiles(data);
            Fragments = GetFragments(files, data, onlyForThisSkeleton);
            AnimationBin = GetAnimationBins(files, data).FirstOrDefault();

            //animations/matched_combat/attila_generated.bin
            var loadedFileCount = Fragments.Count;
            if (AnimationBin != null)
                loadedFileCount++;
            HasUnknownElements = loadedFileCount != files.Count();
        }

        public AnimationPackFile(string fileName)
        {
            FileName = fileName;
        }


        List<AnimationFragment> GetFragments(List<AnimationDataFile> animationDataFiles, ByteChunk data, string onlyForThisSkeleton = null)
        {
            var fragmentFiles = animationDataFiles.Where(x => x.Name.Contains(".frg"));

            var output = new List<AnimationFragment>(fragmentFiles.Count());
            foreach (var fragmentFile in fragmentFiles)
            {
                data.Index = fragmentFile.StartOffset;
                var fragment = new AnimationFragment(fragmentFile.Name, data);
                fragment.ParentAnimationPack = this;
                if (onlyForThisSkeleton != null)
                {
                    if (onlyForThisSkeleton == fragment.Skeletons.Values.FirstOrDefault())
                        output.Add(fragment);
                }
                else
                {
                    output.Add(fragment);
                }
            }

            return output;
        }

        public byte[] GetFile(string path, out AnimationPackFileType type)
        {
            type = GetFileType(path);

            if (AnimationBin != null && AnimationBin.FileName == path)
            {
                return GetAnimBin(path).ToByteArray();
            }

            return GetAnimFragment(path).ToByteArray();


            throw new Exception("File not found");
        }

        public AnimationFragment GetAnimFragment(string path)
        {
            foreach (var item in Fragments)
            {
                if (item.FileName == path)
                {
                    return item;
                }
            }

            throw new Exception("File not found");
        }

        public AnimationBin GetAnimBin(string path)
        {
            if (AnimationBin != null && AnimationBin.FileName == path)
            {
                return AnimationBin;
            }

            throw new Exception("File not found");
        }

        public AnimationPackFileType GetFileType(string path)
        {
            if (AnimationBin != null && AnimationBin.FileName == path)
            {
                return AnimationPackFileType.Bin;
            }

            foreach (var item in Fragments)
            {
                if (item.FileName == path)
                {
                    return AnimationPackFileType.Fragment;
                }
            }

            throw new Exception("File not found");
        }

        public void UpdateFileFromBytes(string path, byte[] bytes)
        {
            if (path.Contains("tables.bin", StringComparison.InvariantCultureIgnoreCase))
            {
                AnimationBin = new AnimationBin(path, new ByteChunk(bytes));
                return;
            }

            if (path.Contains(".frg", StringComparison.InvariantCultureIgnoreCase))
            {
                for (int i = 0; i < Fragments.Count; i++)
                {
                    if (Fragments[i].FileName == path)
                    {
                        Fragments[i] = new AnimationFragment(path, new ByteChunk(bytes));
                        return;
                    }
                }
            }

            throw new Exception("Unable to update file - " + path);
        }

        List<AnimationBin> GetAnimationBins(List<AnimationDataFile> animationDataFile, ByteChunk data)
        {
            var output = new List<AnimationBin>();
            var animationBins = animationDataFile.Where(x => x.Name.Contains("tables.bin")).ToList();

            if (animationBins.Count > 2)
                throw new Exception("This pack contains multiple tables.bin files, not supported");

            foreach (var animBin in animationBins)
            {
                var byteChunk = new ByteChunk(data.Buffer, animBin.StartOffset);
                output.Add(new AnimationBin(animBin.Name, byteChunk));
            }

            return output;
        }

        List<AnimationDataFile> FindAllSubFiles(ByteChunk data)
        {
            var toalFileCount = data.ReadInt32();
            var fileList = new List<AnimationDataFile>(toalFileCount);
            for (int i = 0; i < toalFileCount; i++)
            {
                var file = new AnimationDataFile(data);
                fileList.Add(file);
                data.Index += file.Size;
            }
            return fileList;
        }

        public byte[] ToByteArray()
        {
            if (HasUnknownElements)
                throw new Exception("Can not save animation pack with unkown elements");

            using MemoryStream memStream = new MemoryStream();

            int totalFileCount = Fragments.Count;
            if (AnimationBin != null)
                totalFileCount++;

            memStream.Write(ByteParsers.Int32.EncodeValue(totalFileCount, out _));

            if (AnimationBin != null)
            {
                var animBinByteArray = AnimationBin.ToByteArray();
                AnimationDataFile file = new AnimationDataFile()
                {
                    Name = AnimationBin.FileName,
                    Size = animBinByteArray.Length
                };
                memStream.Write(file.ToByteArray());
                memStream.Write(animBinByteArray);
            }

            foreach (var item in Fragments)
            {
                var itemByteArray = item.ToByteArray();
                AnimationDataFile file = new AnimationDataFile()
                {
                    Name = item.FileName,
                    Size = itemByteArray.Length
                };
                memStream.Write(file.ToByteArray());
                memStream.Write(itemByteArray);
            }

            return memStream.ToArray();
        }
    }
}
