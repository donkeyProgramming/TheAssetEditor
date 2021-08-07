namespace CommonControls.Editors.TextEditor
{
    public interface ITextConverter
    {

        public class SaveError
        {
            public string Text { get; set; }
            public int ErrorLineNumber{ get; set; } = -1;
            public int ErrorPosition { get; set; } = -1;
            public int ErrorLength { get; set; } = -1;
        }

        string GetText(byte[] bytes);
        byte[] ToBytes(string text, string fileName, out SaveError error);

        bool ShouldShowLineNumbers();
        string GetSyntaxType();

    }
}
