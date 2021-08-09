using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.Services;
using Filetypes.ByteParsing;
using FileTypes.AnimationPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.CampaignAnimBin
{
    class CampaignAnimBinToXmlConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                var bin = CampaignAnimationBinLoader.Load(new ByteChunk(bytes));
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true }))
                {
                    xmlserializer.Serialize(writer, bin);
                    var str = stringWriter.ToString();
                    return str;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        public byte[] ToBytes(string text, string filePath, PackFileService pfs, out ITextConverter.SaveError error)
        {
            try
            {
                ITextConverter.SaveError tempError = null;
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                using var stringReader = new StringReader(text);
                var reader = XmlReader.Create(stringReader);
                
                var xmlEventHandler = new XmlDeserializationEvents();
                xmlEventHandler.OnUnknownElement = (x,e) => tempError = new ITextConverter.SaveError() 
                { 
                    Text = "Unsuported xml element : " + e.Element.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}", 
                    ErrorLineNumber = e.LineNumber,
                    ErrorPosition = e.LinePosition - e.Element.LocalName.Length,
                    ErrorLength = e.Element.LocalName.Length
                };

                xmlEventHandler.OnUnknownAttribute = (x, e) => tempError = new ITextConverter.SaveError()
                {
                    Text = "Unsuported xml attribute : " + e.Attr.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}",
                    ErrorLineNumber = e.LineNumber,
                    ErrorPosition = e.LinePosition - e.Attr.LocalName.Length,
                    ErrorLength = e.Attr.LocalName.Length
                };

                xmlEventHandler.OnUnknownNode = (x, e) => tempError = new ITextConverter.SaveError()
                {
                    Text = "Unsuported xml node : " + e.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}",
                    ErrorLineNumber = e.LineNumber,
                    ErrorPosition = e.LinePosition - e.LocalName.Length,
                    ErrorLength = e.LocalName.Length
                }; ;

                var obj = xmlserializer.Deserialize(reader, xmlEventHandler);
                var typedObject = obj as CampaignAnimationBin;
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                var bytes = CampaignAnimationBinLoader.Write(typedObject, fileName);
                if (tempError != null)
                {
                    error = tempError;
                    return null;
                }
                error = null;
                return bytes;

            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else
                    error = new ITextConverter.SaveError() { Text = e.Message };

                return null;
            }
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => false;
    }
}
