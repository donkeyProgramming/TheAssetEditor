﻿using System;
using Shared.GameFormats.WWise;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public interface IWWiseHircGenerator
    {
        public string GameName { get; }
        public Type AudioProjectType { get; }
        public HircItem ConvertToWWise(IAudioProjectHircItem projectItem, CompilerData project);
    }
}
