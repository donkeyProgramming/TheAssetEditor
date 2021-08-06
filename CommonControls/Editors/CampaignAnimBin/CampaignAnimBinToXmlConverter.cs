using CommonControls.Editors.TextEditor;
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

        public byte[] ToBytes(string text)
        {
            throw new NotImplementedException();
        }

        public bool Validate(string text, out string errorText)
        {
            errorText = null;
            return true;
        }
    }
}
