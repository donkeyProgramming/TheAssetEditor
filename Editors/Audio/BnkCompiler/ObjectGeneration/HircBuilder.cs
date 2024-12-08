using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Diagnostics;
using Shared.GameFormats.WWise;
using Shared.GameFormats.WWise.Hirc;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class HircBuilder
    {
        private readonly IEnumerable<IWWiseHircGenerator> _wwiseHircGenerators;

        public HircBuilder(IEnumerable<IWWiseHircGenerator> wwiseHircGenerators)
        {
            _wwiseHircGenerators = wwiseHircGenerators;
        }

        public HircChunk Generate(CompilerData projectFile)
        {
            // Build Hirc list. Order is important! 
            var hircList = ConvertProjectToHircObjects(projectFile);

            var hircChuck = new HircChunk();
            hircChuck.SetFromHircList(hircList);

            // Validate this is same as before.
            hircChuck.ChunkHeader.ChunkSize = (uint)(hircChuck.Hircs.Sum(x => x.Size) + hircChuck.Hircs.Count * 5 + 4);
            hircChuck.NumHircItems = (uint)hircChuck.Hircs.Count;
            return hircChuck;
        }

        private List<HircItem> ConvertProjectToHircObjects(CompilerData project)
        {
            var sortedProjectHircList = HircSorter.Sort(project);
            var wwiseHircs = sortedProjectHircList.Select(hircProjectItem =>
            {
                var generator = FindGenerator(hircProjectItem, project.ProjectSettings.OutputGame);
                return generator.ConvertToWWise(hircProjectItem, project);
            }).ToList();
            return wwiseHircs;
        }

        IWWiseHircGenerator FindGenerator(IAudioProjectHircItem projectItem, string game)
        {
            var generators = new List<IWWiseHircGenerator>();
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
