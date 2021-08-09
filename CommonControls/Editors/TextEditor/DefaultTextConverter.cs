using CommonControls.Services;
using System.IO;
using System.Text;

namespace CommonControls.Editors.TextEditor
{
    public class DefaultTextConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length))
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                    return reader.ReadToEnd();
            }
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => true;

        public byte[] ToBytes(string text, string fileName, PackFileService pfs, out ITextConverter.SaveError error)
        {
            error = null;
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
