using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.AudioProjectExplorer;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.UICommands
{
    public enum AudioProjectCommandAction
    {
        AddToAudioProject,
        RemoveFromAudioProject
    }

    public interface IAudioProjectUICommand : IUiCommand
    {
        AudioProjectCommandAction Action { get; }
        NodeType NodeType { get; }
        void Execute(DataRow row);
    }

    public interface IAudioProjectUICommandFactory
    {
        IAudioProjectUICommand Create(AudioProjectCommandAction action, NodeType nodeType);
    }

    public sealed class AudioProjectUICommandFactory : IAudioProjectUICommandFactory
    {
        private readonly Dictionary<(AudioProjectCommandAction action, NodeType nodeType), IAudioProjectUICommand> _uiCommands;

        public AudioProjectUICommandFactory(IEnumerable<IAudioProjectUICommand> uiCommands)
        {
            _uiCommands = uiCommands.ToDictionary(uiCommand => (uiCommand.Action, uiCommand.NodeType));
        }

        public IAudioProjectUICommand Create(AudioProjectCommandAction action, NodeType nodeType)
        {
            if (!_uiCommands.TryGetValue((action, nodeType), out var uiCommand))
                throw new InvalidOperationException($"No UICommand registered for {nodeType}.");
            return uiCommand;
        }
    }
}
