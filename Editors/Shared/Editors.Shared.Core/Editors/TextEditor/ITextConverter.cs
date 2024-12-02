using System.Xml.Serialization;
using Shared.Core.PackFiles;

namespace Shared.Ui.Editors.TextEditor
{
    public interface ITextConverter
    {
        public class SaveError
        {
            public string Text { get; set; }
            public int ErrorLineNumber { get; set; } = 1;
            public int ErrorPosition { get; set; } = 0;
            public int ErrorLength { get; set; } = 0;
        }

        string GetText(byte[] bytes);
        bool ShouldShowLineNumbers();
        string GetSyntaxType();
        bool CanSaveOnError();
        byte[] ToBytes(string text, string filePath, IPackFileService pfs, out SaveError error);
    }


    public class XmlSerializationErrorHandler
    {
        public ITextConverter.SaveError Error { get; set; } = null;

        public XmlDeserializationEvents EventHandler { get; set; } = new XmlDeserializationEvents();

        public XmlSerializationErrorHandler()
        {
            var item = new XmlDeserializationEvents();
            item.OnUnknownElement = (x, e) => Error = new ITextConverter.SaveError()
            {
                Text = "Unsuported xml element : " + e.Element.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}",
                ErrorLineNumber = e.LineNumber,
                ErrorPosition = e.LinePosition - e.Element.LocalName.Length,
                ErrorLength = e.Element.LocalName.Length
            };

            item.OnUnknownAttribute = (x, e) => Error = new ITextConverter.SaveError()
            {
                Text = "Unsuported xml attribute : " + e.Attr.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}",
                ErrorLineNumber = e.LineNumber,
                ErrorPosition = e.LinePosition - e.Attr.LocalName.Length,
                ErrorLength = e.Attr.LocalName.Length
            };

            item.OnUnknownNode = (x, e) => Error = new ITextConverter.SaveError()
            {
                Text = "Unsuported xml node : " + e.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}",
                ErrorLineNumber = e.LineNumber,
                ErrorPosition = e.LinePosition - e.LocalName.Length,
                ErrorLength = e.LocalName.Length
            };

            EventHandler = item;
        }
    }
}
