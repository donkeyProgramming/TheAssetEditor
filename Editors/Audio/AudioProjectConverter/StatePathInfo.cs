using System.Collections.Generic;
using Editors.Audio.Shared.AudioProject.Models;

namespace Editors.Audio.AudioProjectConverter
{
    public class StatePathInfo
    {
        public string JoinedStatePath { get; set; }
        public List<StatePath.Node> StatePathNodes { get; set; }
        public List<WavFile> WavFiles { get; set; }
    }
}
