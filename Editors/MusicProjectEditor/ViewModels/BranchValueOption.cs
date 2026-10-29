namespace Editors.MusicProjectEditor.ViewModels
{
    /// <summary>A value a tree branch can be added for; a null <paramref name="Value"/> is the
    /// "any other value" branch.</summary>
    public record BranchValueOption(string? Value, string DisplayName);
}
