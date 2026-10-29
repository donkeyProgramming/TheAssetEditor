namespace Editors.MusicDatEditor.ViewModels
{
    /// <summary>Read-only row for the symbol-table reference panels (Triggers, Variables,
    /// StringVariables, StringConstants, Intrinsics) - lets the user look up what a raw
    /// operand index in the instruction grid refers to.</summary>
    public class SymbolListItemViewModel(int index, string name, string value)
    {
        public int Index { get; } = index;
        public string Name { get; } = name;
        public string Value { get; } = value;
    }
}
