using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Editors.ImportExport.Common;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Presentation
{
    public interface IImporterViewModel
    {
        public string DisplayName { get; }
        string OutputExtension { get; }
        string[] InputExtensions { get; } // ADDed THIS!
        public void Execute(PackFile exportSource, string outputPath, PackFileContainer packFileContainer, GameTypeEnum gameType);
        public ImportExportSupportEnum CanImportFile(PackFile file);

    }
}
