using Shared.GameFormats.MusicDat;

namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>Row for the function list (call-graph level). Wraps the disassembled
    /// <see cref="MusicDatDisassembler.Function"/> so the instruction grid can be rebuilt
    /// from it when selected.</summary>
    public class FunctionListItemViewModel(MusicDatDisassembler.Function function)
    {
        public MusicDatDisassembler.Function Function { get; } = function;
        public string Name => Function.Name;
        public int Start => Function.Start;
        public int InstructionCount => Function.Instructions.Count;
        public bool DecodedExactly => Function.DecodedExactly;
        public bool IsKnownEntryPoint => Function.IsKnownEntryPoint;
        public bool IsLikelyUnreachable => Function.IsLikelyUnreachable;

        public string DisplayName => IsKnownEntryPoint ? $"▶ {Name}" : Name;
    }
}
