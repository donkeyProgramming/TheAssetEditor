using Common;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTypes.AnimationPack
{
    public class AnimationPackFile
    {
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

        public AnimationPackFile(PackFile file)
        {
            var data = file.DataSource.ReadDataAsChunk();

            var files = FindAllSubFiles(data);
            Fragments = GetFragments(files, data);
            AnimationBin = GetAnimationBins(files, data).FirstOrDefault();
            var loadedFileCount = Fragments.Count;
            if (AnimationBin != null)
                loadedFileCount++;
            HasUnknownElements = loadedFileCount != files.Count();
        }

        public AnimationPackFile()
        { }


        List<AnimationFragment> GetFragments(List<AnimationDataFile> animationDataFiles, ByteChunk data)
        {
            var fragmentFiles = animationDataFiles.Where(x => x.Name.Contains(".frg"));

            var output = new List<AnimationFragment>();
            foreach (var fragmentFile in fragmentFiles)
            {
                data.Index = fragmentFile.StartOffset;
                output.Add(new AnimationFragment(fragmentFile.Name, data));
            }

            return output;
        }

        List<AnimationBin> GetAnimationBins(List<AnimationDataFile> animationDataFile, ByteChunk data)
        {
            var output = new List<AnimationBin>();
            var animationBins = animationDataFile.Where(x => x.Name.Contains("tables.bin")).ToList();

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
