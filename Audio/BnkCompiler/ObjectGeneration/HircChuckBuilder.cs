using Audio.BnkCompiler.ObjectGeneration.Warhammer3;
using Audio.FileFormats.WWise;
using Audio.FileFormats.WWise.Hirc;
using Audio.FileFormats.WWise.Hirc.V136;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System.Collections.Generic;
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

        public HircChunk Generate(AudioInputProject projectFile)
        {
            // Build Hirc list. Order is important! 
            var repo = BuildRepository(projectFile);
            var hircList = ConvertProjectToHircObjects(projectFile, repo);

            var hircChuck = new HircChunk();
            hircChuck.SetFromHircList(hircList);

            // Validate this is same as before
            hircChuck.ChunkHeader.ChunkSize = (uint)(hircChuck.Hircs.Sum(x => x.Size) + hircChuck.Hircs.Count() * 5 + 4);
            hircChuck.NumHircItems = (uint)hircChuck.Hircs.Count();

            return hircChuck;
        }

        private static HircProjectItemRepository BuildRepository(AudioInputProject projectFile)
        {
            var repo = new HircProjectItemRepository();
            repo.AddCollection(projectFile.Events);
            repo.AddCollection(projectFile.Actions);
            repo.AddCollection(projectFile.GameSounds);
            return repo;
        }

        private List<HircItem> ConvertProjectToHircObjects(AudioInputProject project, HircProjectItemRepository repo)
        {
            var hircList = new List<HircItem>();
            hircList.AddRange(Procsss(project.GameSounds, repo));
            hircList.AddRange(Procsss(project.Actions, project.ProjectSettings.BnkName, repo));
            hircList.AddRange(Procsss(project.Events, repo));
            return hircList;
        }

        List<CAkAction_v136> Procsss(List<Action> audioEvent, string bnkName, HircProjectItemRepository repository) => _actionGenerator.ConvertToWWise(audioEvent, bnkName, repository);
        List<CAkEvent_v136> Procsss(List<Event> audioEvent, HircProjectItemRepository repository) => _eventGenerator.ConvertToWWise(audioEvent, repository);
        List<CAkSound_v136> Procsss(List<GameSound> audioEvent, HircProjectItemRepository repository) => _soundGenerator.ConvertToWWise(audioEvent, repository);
    }
}
