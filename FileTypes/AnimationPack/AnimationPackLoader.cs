using Common;
using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTypes.AnimationPack
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

        static public IEnumerable<AnimationFragment> GetFragments(PackFile file)
        {
            var data = file.DataSource.ReadDataAsChunk();
            var fragmentFiles = FindAllSubFiles(data).Where(x => x.Name.Contains(".frg"));

            var output = new List<AnimationFragment>();
            foreach (var fragmentFile in fragmentFiles)
            {
                data.Index = fragmentFile.StartOffset;
                output.Add(new AnimationFragment(fragmentFile.Name, data));
            }

            return output;
        }

        static public List<AnimationBin> GetAnimationBins(PackFile file)
        {
            var output = new List<AnimationBin>();
            var data = file.DataSource.ReadDataAsChunk();
            var animationBins = FindAllSubFiles(data).Where(x => x.Name.Contains("tables.bin")).ToList();

            foreach (var animBin in animationBins)
            {
                var byteChunk = new ByteChunk(data.Buffer, animBin.StartOffset);
                output.Add(new AnimationBin(animBin.Name, byteChunk));
            }

            return output;
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
    }
}
