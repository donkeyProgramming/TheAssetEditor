using Shared.GameFormats.WWise;
using System;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public interface IWWiseHircGenerator
    {
        public string GameName { get; }
        public Type AudioProjectType { get; }
        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project);
    }
}
