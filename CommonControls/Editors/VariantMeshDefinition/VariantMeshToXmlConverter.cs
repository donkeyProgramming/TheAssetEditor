// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using CommonControls.Common;
using CommonControls.Editors.TextEditor;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using static CommonControls.FileTypes.Vmd.VariantMeshDefinition;

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

        public byte[] ToBytes(string text, string filePath, PackFileService pfs, out ITextConverter.SaveError error)
        {
            try
            {
                // Validate
                var obj = Load(text); ;
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


        public static VariantMesh Load(string fileContent, bool strict = false)
        {
            XmlRootAttribute xRoot = new XmlRootAttribute("VARIANT_MESH");

            var xmlserializer = new XmlSerializer(typeof(VariantMesh), xRoot);
            using var stringReader = new StringReader(fileContent);
            var reader = XmlReader.Create(stringReader);

            object result = null;
            if (strict)
                result = xmlserializer.Deserialize(reader, new UnknownXmlDataThrower().EventHandler);
            else
                result = xmlserializer.Deserialize(reader);

            var typedObject = result as VariantMesh;
            typedObject.FixStrings();
            return typedObject;
        }

        public static VariantMesh Load(PackFile pf, bool strict = false)
        {
            var vmdContent = Encoding.UTF8.GetString(pf.DataSource.ReadData());
            return Load(vmdContent, strict);
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => true;
    }
}