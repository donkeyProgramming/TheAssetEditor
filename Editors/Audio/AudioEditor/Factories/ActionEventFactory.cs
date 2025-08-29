using System.Collections.Generic;
using Editors.Audio.AudioEditor.Models;
using Editors.Audio.AudioEditor.Settings;
using Editors.Audio.GameInformation.Warhammer3;

namespace Editors.Audio.AudioEditor.Factories
{
    public interface IActionEventFactory
    {
        ActionEvent Create(Wh3ActionEventType actionEventGroup, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings);
    }

    public class ActionEventFactory(ISoundFactory soundFactory, IRandomSequenceContainerFactory randomSequenceContainerFactory) : IActionEventFactory
    {
        private readonly ISoundFactory _soundFactory = soundFactory;
        private readonly IRandomSequenceContainerFactory _randomSequenceContainerFactory = randomSequenceContainerFactory;

        public ActionEvent Create(Wh3ActionEventType actionEventGroup, string actionEventName, List<AudioFile> audioFiles, AudioSettings audioSettings)
        {
            var actionEvent = new ActionEvent();
            if (audioFiles.Count == 1)
            {
                var sound = _soundFactory.Create(audioFiles[0], audioSettings);
                actionEvent = ActionEvent.Create(actionEventName, sound, actionEventGroup);
            }
            else if (audioFiles.Count > 1)
            {
                var randomSequenceContainer = _randomSequenceContainerFactory.Create(audioSettings, audioFiles);
                actionEvent = ActionEvent.Create(actionEventName, randomSequenceContainer, actionEventGroup);
            }
            return actionEvent;
        }
    }
}
