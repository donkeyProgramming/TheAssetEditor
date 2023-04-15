using Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircChuckBuilder
    {

        private readonly ActionGenerator _actionGenerator;
        private readonly EventGenerator _eventGenerator;
        private readonly GameSoundGenerator _soundGenerator;

        public HircChuckBuilder(ActionGenerator actionGenerator, EventGenerator eventGenerator, GameSoundGenerator soundGenerator)
        {
            _actionGenerator = actionGenerator;
            _eventGenerator = eventGenerator;
            _soundGenerator = soundGenerator;
        }

        public HircChunk Generate(AudioProjectXml projectFile)
        {
            var bnkName = Path.GetFileNameWithoutExtension(projectFile.OutputFile);

            // Build Hirc list. Order is important! 
            var repo = BuildRepository(projectFile);
            var hircList = ConvertProjectToHircObjects(projectFile, repo, bnkName);

            var hircChuck = new HircChunk();
            hircChuck.SetFromHircList(hircList);

            // Validate this is same as before
            hircChuck.ChunkHeader.ChunkSize = (uint)(hircChuck.Hircs.Sum(x => x.Size) + hircChuck.Hircs.Count() * 5 + 4);
            hircChuck.NumHircItems = (uint)hircChuck.Hircs.Count();

            return hircChuck;
        }

        private static HircProjectItemRepository BuildRepository(AudioProjectXml projectFile)
        {
            var repo = new HircProjectItemRepository();
            repo.AddCollection(projectFile.Events);
            repo.AddCollection(projectFile.Actions);
            repo.AddCollection(projectFile.GameSounds);
            return repo;
        }

        private List<HircItem> ConvertProjectToHircObjects(AudioProjectXml project, HircProjectItemRepository repo, string bnkName)
        {
            var hircList = new List<HircItem>();
            hircList.AddRange(_generatorFactory.Procsss(project.GameSounds, repo));
            hircList.AddRange(_generatorFactory.Procsss(project.Actions, bnkName, repo));
            hircList.AddRange(_generatorFactory.Procsss(project.Events, repo));
            return hircList;
        }




        public List<CAkAction_v136> Procsss(List<Action> audioEvent, string bnkName, HircProjectItemRepository repository) => _actionGenerator.ConvertToWWise(audioEvent, bnkName, repository);
        public List<CAkEvent_v136> Procsss(List<Event> audioEvent, HircProjectItemRepository repository) => _eventGenerator.ConvertToWWise(audioEvent, repository);
        public List<CAkSound_v136> Procsss(List<GameSound> audioEvent, HircProjectItemRepository repository) => _soundGenerator.ConvertToWWise(audioEvent, repository);
    }
}
