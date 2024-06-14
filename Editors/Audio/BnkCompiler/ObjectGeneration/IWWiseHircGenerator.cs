using Editors.Audio.BnkCompiler;
using Shared.GameFormats.WWise;
using System;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public interface IWWiseHircGenerator
    {
        public string GameName { get; }
        public Type AudioProjectType { get; }
        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project);
    }
}
