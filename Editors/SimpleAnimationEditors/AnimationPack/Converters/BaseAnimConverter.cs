using System.Collections;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Ui.Editors.TextEditor;

namespace CommonControls.Editors.AnimationPack.Converters
{
    public abstract class BaseAnimConverter<XmlType, AnimType> : ITextConverter
        where AnimType : class
        where XmlType : class
    {
        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;

        public PackFile AnimPackToValidate = null;

        protected abstract ITextConverter.SaveError Validate(XmlType type, string s, IPackFileService pfs, string filepath);
        protected abstract XmlType ConvertBytesToXmlClass(byte[] bytes);
        protected abstract byte[] ConvertToAnimClassBytes(XmlType xmlType, string path);
        protected virtual string CleanUpXml(string xmlText) => xmlText;


        public string GetText(byte[] bytes)
        {
            try
            {
                var xmlFrg = ConvertBytesToXmlClass(bytes);

                var xmlserializer = new XmlSerializer(typeof(XmlType));
                var stringWriter = new StringWriter();
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
                {
                    xmlserializer.Serialize(writer, xmlFrg, ns);
                    var str = stringWriter.ToString();
                    str = CleanUpXml(str);
                    return str;
                }
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

                return ConvertToAnimClassBytes(obj, filePath);
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

        protected ITextConverter.SaveError GenerateError(string wholeText, int lastIndex, string errorMessage)
        {
            var array = wholeText.ToCharArray();
            var lineCount = 0;
            for (int strIndex = 0; strIndex < lastIndex; strIndex++)
            {
                if (array[strIndex] == '\n')
                    lineCount++;
            }

            return new ITextConverter.SaveError() { ErrorLength = 40, ErrorPosition = 0, ErrorLineNumber = lineCount, Text = errorMessage };
        }

        protected bool ValidateBoolArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split(",");
            if (parts.Length != 6)
                return false;

            for (int i = 0; i < 6; i++)
            {
                var str = parts[i].Trim();
                if (bool.TryParse(str, out _) == false)
                    return false;
            }
            return true;
        }

        protected string ConvertIntToBoolArray(int value)
        {
            var bitArray = new BitArray(new int[] { value });
            var bits = new bool[bitArray.Count];
            bitArray.CopyTo(bits, 0);

            string[] strArray = new string[6];
            for (int i = 0; i < 6; i++)
                strArray[i] = bits[i].ToString();

            return string.Join(", ", strArray);
        }

        protected int CreateWeaponFlagInt(string strArray)
        {
            var values = strArray.Split(",").Select(x => bool.Parse(x)).ToArray();
            BitArray b = new BitArray(new int[] { 0 });
            for (int i = 0; i < 6; i++)
                b[i] = values[i];

            int[] array = new int[1];
            b.CopyTo(array, 0);
            return array[0];
        }
    }
}
