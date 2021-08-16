using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using static Filetypes.RigidModel.VariantMeshDefinition;

namespace CommonControls.Editors.VariantMeshDefinition
{
    public class VariantMeshToXmlConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to open file\n" + e.Message);
                return "";
            }
        }

        public byte[] ToBytes(string text, string filePath, PackFileService pfs,  out ITextConverter.SaveError error)
        {
            try
            {
                // Validate
                var obj = LoadFromString(text);;
                error = ValidateFilePaths(obj, pfs);
               
                return Encoding.UTF8.GetBytes(text);
            }
            catch (Exception e)
            {
                var inner = ExceptionHelper.GetInnerMostException(e);
                if (inner is XmlException xmlException)
                    error = new ITextConverter.SaveError() { Text = xmlException.Message, ErrorLineNumber = xmlException.LineNumber, ErrorPosition = xmlException.LinePosition, ErrorLength = 0 };
                else
                    error = new ITextConverter.SaveError() { Text = e.Message };

                return Encoding.UTF8.GetBytes(text);
            }
        }

        ITextConverter.SaveError ValidateFilePaths(VariantMesh mesh, PackFileService pfs)
        {
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) == false)
            {
                if (pfs.FindFile(mesh.ModelReference) == null)
                    return new ITextConverter.SaveError() { Text = $"Unabel to find file '{mesh.ModelReference}'", ErrorLineNumber = 1, ErrorPosition = 0, ErrorLength = 0 };
            }

            foreach (var slot in mesh.ChildSlots)
            {
                foreach (var item in slot.ChildMeshes)
                {
                    var res = ValidateFilePaths(item, pfs);
                    if (res != null)
                        return res;
                }

                foreach (var item in slot.ChildReferences)
                {
                    if (string.IsNullOrWhiteSpace(mesh.ModelReference) == false)
                    {
                        if (pfs.FindFile(item.Reference) == null)
                            return new ITextConverter.SaveError() { Text = $"Unabel to find file {item.Reference}", ErrorLineNumber = 1, ErrorPosition = 0, ErrorLength = 0 };
                    }
                }
            }

            return null;
        }


        public static VariantMesh LoadFromString(string fileContent)
        {
            XmlRootAttribute xRoot = new XmlRootAttribute("VARIANT_MESH");

            var xmlserializer = new XmlSerializer(typeof(VariantMesh), xRoot);
            using var stringReader = new StringReader(fileContent);
            var reader = XmlReader.Create(stringReader);

            var obj = xmlserializer.Deserialize(reader, new UnkownXmlDataThrower().EventHandler);
            var typedObject = obj as VariantMesh;
            typedObject.FixStrings();
            return typedObject;
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => true;
    }
}