using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Ui.Editors.TextEditor;

namespace Editors.AnimationFragmentEditor.AnimationPack.Converters
{
    public abstract class XmlToBinaryConverter<XmlType, BinaryType> : ITextConverter
        where BinaryType : class
        where XmlType : class
    {
        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;

        protected abstract ITextConverter.SaveError Validate(XmlType type, string s, IPackFileService pfs, string filepath);
        protected abstract XmlType ConvertBinaryToXml(byte[] bytes);
        protected abstract byte[] ConvertXmlToBinary(XmlType xmlType, string path);
        protected virtual string CleanUpXml(string xmlText) => xmlText;

        public string GetText(byte[] bytes)
        {
            try
            {
                var xmlFrg = ConvertBinaryToXml(bytes);

                var xmlserializer = new XmlSerializer(typeof(XmlType));
                using var stringWriter = new StringWriter();
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                using var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true });
                xmlserializer.Serialize(writer, xmlFrg, ns);
                var str = stringWriter.ToString();
                str = CleanUpXml(str);
                return str;
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        public byte[] ToBytes(string text, string filePath, IPackFileService pfs, out ITextConverter.SaveError error)
        {
            var xmlserializer = new XmlSerializer(typeof(XmlType));
            using var sr = new StringReader(text);
            using var reader = XmlReader.Create(sr);

            try
            {
                var errorHandler = new XmlSerializationErrorHandler();
                var obj = xmlserializer.Deserialize(reader, errorHandler.EventHandler) as XmlType;

                if (errorHandler.Error != null)
                {
                    error = errorHandler.Error;
                    return null;
                }

                error = Validate(obj, text, pfs, filePath);
                if (error != null)
                    return null;

                return ConvertXmlToBinary(obj, filePath);
            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else
                    throw;
  
                return null;
            }
        }
    }
}
