namespace Shared.Ui.Editors.BoneMapping
{
    public class IndexRemapping
    {
        public IndexRemapping(int originalValue, int newValue, bool isUsedByCurrentModel = false)
        {
            OriginalValue = originalValue;
            NewValue = newValue;
            IsUsedByModel = isUsedByCurrentModel;
        }

        public int OriginalValue { get; set; }
        public int NewValue { get; set; }
        public bool IsUsedByModel { get; set; } = false;
    }
}
