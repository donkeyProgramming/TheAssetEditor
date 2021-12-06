using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using Filetypes.ByteParsing;
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
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                using var stringReader = new StringReader(text);
                var reader = XmlReader.Create(stringReader);

                var errorHandler = new XmlSerializationErrorHandler();

                var obj = xmlserializer.Deserialize(reader, errorHandler.EventHandler);
                var typedObject = obj as CampaignAnimationBin;
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                var bytes = CampaignAnimationBinLoader.Write(typedObject, fileName);
                if (errorHandler.Error != null)
                {
                    error = errorHandler.Error;
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
