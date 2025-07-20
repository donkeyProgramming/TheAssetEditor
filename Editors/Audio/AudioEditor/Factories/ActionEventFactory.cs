using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IActionEventFactory
    {
        ActionEvent Create(string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
    }

    public class ActionEventFactory : IActionEventFactory
    {
        private readonly ISoundFactory _soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory;

        public ActionEventFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory)
        {
            _soundFactory = soundFactory;
            _randomSequenceContainerFactory = randomSequenceContainerFactory;
        }

        public ActionEvent Create(string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var actionEvent = new ActionEvent();
            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(audioFiles[0], audioSettings);
                actionEvent = ActionEvent.Create(actionEventName, sound);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(audioSettings, audioFiles);
                actionEvent = ActionEvent.Create(actionEventName, randomSequenceContainer);
            }
            return actionEvent;
        }
    }
}
