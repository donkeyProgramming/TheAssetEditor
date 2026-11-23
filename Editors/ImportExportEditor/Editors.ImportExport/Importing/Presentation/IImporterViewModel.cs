using Editors.ImportExport.Misc;
using Shared.Core.PackFiles.Models;
using Shared.Core.Settings;

namespace Editors.ImportExport.Importing.Presentation
{
    public interface IImporterViewModel
    {
        public string DisplayName { get; }
        string OutputExtension { get; }
        string[] InputExtensions { get; }
        bool SupportsCancellation => false;

        /// <summary>
        /// Called once the input file is known, so the importer can fill in defaults from it.
        /// </summary>
        void Initialize(PackFile inputFile) { }

        /// <summary>
        /// Runs on a background thread. Progress and cancellation are optional for importers that don't support them.
        /// </summary>
        ImportResult Execute(PackFile importSource, string outputPath, IPackFileContainer packFileContainer, GameTypeEnum gameType, IProgress<ImportProgress>? progress, CancellationToken cancellationToken);

        public ImportSupportEnum CanImportFile(PackFile file);
    }
}
