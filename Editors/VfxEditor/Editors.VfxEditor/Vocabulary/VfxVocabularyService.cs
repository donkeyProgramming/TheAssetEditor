using Editors.VfxEditor.Document;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.VfxEditor.Vocabulary
{
    /// <summary>Builds the vocabulary from the CA packs' vfx files on a background thread when a VFX editor first asks, and
    /// again for the next editor if that scan failed or found nothing. A file that does not read adds nothing.</summary>
    public sealed class VfxVocabularyService(IPackFileService packFileService)
    {
        readonly object _lock = new();
        Task<VfxVocabulary>? _task;

        public Task<VfxVocabulary> Get()
        {
            lock (_lock)
            {
                if (_task is { IsFaulted: true } or { IsCompletedSuccessfully: true, Result.EffectNames.Count: 0 })
                    _task = null;
                return _task ??= Task.Run(Build);
            }
        }

        VfxVocabulary Build()
        {
            var files = packFileService.GetAllPackfileContainers().Where(c => c.IsCaPackFile)
                .SelectMany(c => packFileService.FindAllWithExtention(".xml", c))
                .Where(f => VfxVocabulary.EffectName(f.FileName) != null);
            return VfxVocabulary.Build(Read(files));
        }

        static IEnumerable<(string, VfxDocument)> Read(IEnumerable<(string FileName, PackFile Pack)> files)
        {
            foreach (var (path, file) in files)
            {
                VfxDocument? document = null;
                try
                {
                    document = VfxDocument.Load(file.DataSource.ReadData());
                }
                catch (Exception)   // a pack moved on disk too, not only a file that does not parse
                {
                }
                if (document != null)
                    yield return (path, document);
            }
        }
    }
}
