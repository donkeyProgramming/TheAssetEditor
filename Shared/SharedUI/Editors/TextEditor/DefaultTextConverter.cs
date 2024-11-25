// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Shared.Core.PackFiles;

namespace Shared.Ui.Editors.TextEditor
{
    public class DefaultTextConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, 0, bytes.Length))
            {
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                    return reader.ReadToEnd();
            }
        }

        public bool ShouldShowLineNumbers() => true;
        public string GetSyntaxType() => "XML";
        public bool CanSaveOnError() => true;

        public byte[] ToBytes(string text, string fileName, IPackFileService pfs, out ITextConverter.SaveError error)
        {
            error = null;
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
