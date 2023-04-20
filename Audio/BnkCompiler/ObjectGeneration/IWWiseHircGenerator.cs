using Audio.FileFormats.WWise;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using System;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public interface IWWiseHircGenerator
    {
        public string GameName { get; }
        public Type AudioProjectType { get; }
        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, AudioInputProject project, HircProjectItemRepository repository);
    }
}
