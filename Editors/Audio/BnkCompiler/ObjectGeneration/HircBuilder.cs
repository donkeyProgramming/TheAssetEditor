using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class HircBuilder
    {
        private readonly IEnumerable<IWwiseHircGenerator> _wwiseHircGenerators;

        public HircBuilder(IEnumerable<IWwiseHircGenerator> wwiseHircGenerators)
        {
            _wwiseHircGenerators = wwiseHircGenerators;
        }

        public HircChunk Generate(CompilerData projectFile)
        {
            // Build Hirc list. Order is important! 
            var hircList = ConvertProjectToHircObjects(projectFile);

            var hircChunk = new HircChunk();
            hircChunk.SetFromHircList(hircList);

            // Validate this is same as before.
            hircChunk.ChunkHeader.ChunkSize = (uint)(hircChunk.HircItems.Sum(x => x.Size) + hircChunk.HircItems.Count * 5 + 4);
            hircChunk.NumHircItems = (uint)hircChunk.HircItems.Count;
            return hircChunk;
        }

        private List<HircItem> ConvertProjectToHircObjects(CompilerData project)
        {
            var sortedProjectHircList = HircSorter.Sort(project);
            var wwiseHircItems = sortedProjectHircList.Select(hircProjectItem =>
            {
                var generator = FindGenerator(hircProjectItem, project.ProjectSettings.OutputGame);
                return generator.ConvertToWwise(hircProjectItem, project);
            }).ToList();
            return wwiseHircItems;
        }

        private IWwiseHircGenerator FindGenerator(IAudioProjectHircItem projectItem, string game)
        {
            var generators = new List<IWwiseHircGenerator>();
            foreach (var generator in _wwiseHircGenerators)
            {
                if (generator.GameName.Equals(game, StringComparison.InvariantCultureIgnoreCase) && generator.AudioProjectType == projectItem.GetType())
                    generators.Add(generator);
            }
            Guard.IsEqualTo(generators.Count, 1);
            return generators.First();
        }
    }
}
