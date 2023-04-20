using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HichBuilder
    {
        private readonly IEnumerable<IWWiseHircGenerator> _wwiseHircGenerators;
        private readonly HircSorter _hircSorter = new HircSorter();
       
        public HichBuilder(IEnumerable<IWWiseHircGenerator> wwiseHircGenerators)
        {
            _wwiseHircGenerators = wwiseHircGenerators;
        }

        public HircChunk Generate(AudioInputProject projectFile)
        {
            // Build Hirc list. Order is important! 
            var repo = new HircProjectItemRepository(projectFile);
            var hircList = ConvertProjectToHircObjects(projectFile, repo);
           
            var hircChuck = new HircChunk();
            hircChuck.SetFromHircList(hircList);

            // Validate this is same as before
            hircChuck.ChunkHeader.ChunkSize = (uint)(hircChuck.Hircs.Sum(x => x.Size) + hircChuck.Hircs.Count() * 5 + 4);
            hircChuck.NumHircItems = (uint)hircChuck.Hircs.Count();

            return hircChuck;
        }

        private List<HircItem> ConvertProjectToHircObjects(AudioInputProject project, HircProjectItemRepository repo)
        {
            var sortedProjectHircList = _hircSorter.Sort(project);
            var wwiseHircs = sortedProjectHircList.Select(hircProjectItem =>
            {
                var generator = FindGenerator(hircProjectItem, project.ProjectSettings.OutputGame);
                return generator.ConvertToWWise(hircProjectItem, project, repo);
            }).ToList();
            return wwiseHircs;
        }

        IWWiseHircGenerator FindGenerator(IAudioProjectHircItem projectItem, string game)
        {
            var generators = _wwiseHircGenerators.Where(x => x.GameName.Equals(game, StringComparison.InvariantCultureIgnoreCase) && x.AudioProjectType == projectItem.GetType()).ToList();
            Guard.IsEqualTo(generators.Count(), 1);
            return generators.First();  
        }
    }
}
