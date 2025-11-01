using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Editors.Audio.AudioEditor.Presentation.Shared;
using Shared.Core.Events;

namespace Editors.Audio.AudioEditor.Commands
{
    public enum MutationType
    {
        Add,
        Remove
    }

    public interface IAudioProjectMutationUICommand : IUiCommand
    {
        MutationType Action { get; }
        AudioProjectTreeNodeType NodeType { get; }
        void Execute(DataRow row);
    }

    public interface IAudioProjectMutationUICommandFactory
    {
        IAudioProjectMutationUICommand Create(MutationType action, AudioProjectTreeNodeType nodeType);
    }

    public sealed class AudioProjectMutationUICommandFactory(IEnumerable<IAudioProjectMutationUICommand> uiCommands) : IAudioProjectMutationUICommandFactory
    {
        private readonly Dictionary<(MutationType action, AudioProjectTreeNodeType nodeType), IAudioProjectMutationUICommand> _uiCommands = 
            uiCommands.ToDictionary(uiCommand => (uiCommand.Action, uiCommand.NodeType));

        public IAudioProjectMutationUICommand Create(MutationType action, AudioProjectTreeNodeType nodeType)
        {
            if (!_uiCommands.TryGetValue((action, nodeType), out var uiCommand))
                throw new InvalidOperationException($"No UICommand registered for {nodeType}.");
            return uiCommand;
        }
    }
}
