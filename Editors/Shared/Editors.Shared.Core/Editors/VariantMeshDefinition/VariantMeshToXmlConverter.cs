using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Shared.Core.ErrorHandling;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Vmd;
using Shared.Ui.Editors.TextEditor;
using static Shared.GameFormats.Vmd.VariantMeshDefinition;

namespace Shared.Ui.Editors.VariantMeshDefinition
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

        public byte[] ToBytes(string text, string filePath, IPackFileService pfs, out ITextConverter.SaveError error)
        {
            try
            {
                // Validate
                var obj = VariantMeshDefinitionLoader.Load(text); ;
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

        ITextConverter.SaveError ValidateFilePaths(VariantMesh mesh, IPackFileService pfs)
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


       

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => true;
    }
}
