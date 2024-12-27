namespace KitbasherEditor.ViewModels.SaveDialog
{
    public class ComboBoxItem<T> where T : Enum
    {
        public T Value { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; } = "";

        public ComboBoxItem(T value, string displayName, string description) 
        {
            Value = value;
            DisplayName = displayName;
            Description = description;
        }
    }
}
