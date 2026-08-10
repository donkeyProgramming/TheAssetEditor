using System.IO;
using System.Text;
using Shared.ByteParsing;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.PackFiles.Models.FileSources;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Didx;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.Shared.Storage
{
    internal sealed record BnkFileReference(string Path, PackFile File, bool IsCA);

    internal sealed record BnkHircReference(
        uint Id,
        AkBkHircType HircType,
        string BnkPath,
        long Offset,
        int Length,
        uint IndexInBnk,
        uint BankGeneratorVersion,
        uint LanguageId,
        bool IsCA);

    internal sealed record BnkDidxReference(uint Id, string BnkPath, uint LanguageId, long Offset, int Length);

    public class BnkLoader(IPackFileService packFileService)
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly ILogger _logger = Logging.Create<BnkLoader>();

        public BnkFile LoadBnkFile(PackFile bnkFile, string bnkFilePath, bool isCA, bool printData = false)
        {
            var bnk = BnkFile.CreateFromBytes(bnkFile.DataSource.ReadData(), bnkFilePath, isCA);
            if (printData && bnk.HircChunk != null)
                PrintHircList(bnk.HircChunk.HircItems, bnkFilePath);
            return bnk;
        }

        internal static List<BnkFileReference> FindBnkFiles(List<IPackFileContainer> containers)
        {
            var resolvedBnks = new Dictionary<string, BnkFileReference>(StringComparer.OrdinalIgnoreCase);

            foreach (var container in containers)
            {
                foreach (var (path, file) in container.SearchFiles(null, [".bnk"]))
                {
                    if (IsWantedBnk(path))
                        resolvedBnks[path] = new BnkFileReference(path, file, container.IsCaPackFile);
                }
            }

            return resolvedBnks.Values.ToList();
        }

        internal BnkFile.Index LoadIndex(PackFile bnk, string bnkPath)
        {
            var source = bnk.DataSource;
            var decodedSize = GetDecodedSize(source);
            if (TryGetDirectReadLocation(bnk, bnkPath, out var filePath, out var fileOffset))
                return BnkFile.BuildIndex(bnkPath, decodedSize, (offset, length) => ReadFileRange(filePath, checked(fileOffset + offset), length));

            var decodedData = source.ReadData();
            return BnkFile.BuildIndex(bnkPath, decodedData.Length, (offset, length) => ReadByteArrayRange(decodedData, offset, length));
        }

        internal List<HircItem> LoadHircs(List<BnkHircReference> references)
        {
            var result = new List<HircItem>(references.Count);
            foreach (var bnkReferences in references.GroupBy(x => x.BnkPath, StringComparer.OrdinalIgnoreCase))
            {
                var bnk = FindBnk(bnkReferences.Key);
                if (bnk == null)
                    continue;

                var orderedReferences = bnkReferences.OrderBy(x => x.Offset).ToList();
                var rangeOffset = orderedReferences[0].Offset;
                var rangeEnd = orderedReferences.Max(x => checked(x.Offset + x.Length));
                var rangeLength = checked(rangeEnd - rangeOffset);
                if (rangeLength > int.MaxValue)
                {
                    _logger.Here().Warning($"HIRC range in '{bnkReferences.Key}' is too large to load");
                    continue;
                }

                try
                {
                    var bnkRange = ReadData(bnk, bnkReferences.Key, rangeOffset, (int)rangeLength);
                    foreach (var reference in orderedReferences)
                    {
                        var hirc = LoadHirc(bnkRange, rangeOffset, reference);
                        if (hirc != null)
                            result.Add(hirc);
                    }
                }
                catch (Exception exception)
                {
                    _logger.Here().Warning($"Failed to load HIRC range from '{bnkReferences.Key}': {exception.Message}");
                }
            }

            return result;
        }

        internal DidxAudio LoadDidx(BnkDidxReference reference)
        {
            try
            {
                var bnk = FindBnk(reference.BnkPath);
                if (bnk == null)
                    return null;

                return new DidxAudio
                {
                    Id = reference.Id,
                    ByteArray = ReadData(bnk, reference.BnkPath, reference.Offset, reference.Length),
                    OwnerFilePath = reference.BnkPath,
                    LanguageId = reference.LanguageId
                };
            }
            catch (Exception exception)
            {
                _logger.Here().Warning($"Failed to read embedded WEM {reference.Id} from '{reference.BnkPath}' at offset {reference.Offset}: {exception.Message}");
                return null;
            }
        }

        private HircItem LoadHirc(byte[] bnkRange, long rangeOffset, BnkHircReference reference)
        {
            try
            {
                return ParseHirc(bnkRange, rangeOffset, reference);
            }
            catch (Exception exception)
            {
                _logger.Here().Warning($"Failed to read HIRC {reference.Id} from '{reference.BnkPath}' at offset {reference.Offset}: {exception.Message}");
                return null;
            }
        }

        private PackFile FindBnk(string bnkPath)
        {
            var bnk = _packFileService.FindFile(bnkPath);
            if (bnk == null)
                _logger.Here().Warning($"Audio cache references missing sound bank '{bnkPath}'");
            return bnk;
        }

        private static HircItem ParseHirc(byte[] bnkRange, long rangeOffset, BnkHircReference reference)
        {
            var relativeOffset = checked(reference.Offset - rangeOffset);
            if (relativeOffset < 0 || relativeOffset > int.MaxValue || reference.Length > bnkRange.Length - relativeOffset)
                throw new InvalidDataException( $"HIRC range in '{reference.BnkPath}' is outside the supplied bank data.");

            var hirc = HircItem.ReadData(
                reference.BnkPath,
                new ByteChunk(bnkRange, (int)relativeOffset),
                reference.BankGeneratorVersion,
                reference.LanguageId,
                reference.IsCA,
                reference.IndexInBnk,
                reference.Length);
            hirc.IndexInFile = reference.IndexInBnk;
            hirc.BnkFilePath = reference.BnkPath;
            hirc.LanguageId = reference.LanguageId;
            hirc.IsCA = reference.IsCA;
            hirc.ByteIndexInFile = checked((uint)reference.Offset);

            if (hirc.Id != reference.Id || hirc.HircType != reference.HircType)
                throw new InvalidDataException($"HIRC index mismatch in '{reference.BnkPath}' at offset {reference.Offset}.");

            return hirc;
        }

        private static bool IsWantedBnk(string path)
        {
            var normalizedPath = path.Replace('/', '\\');
            return !normalizedPath.Contains(@"\media\", StringComparison.OrdinalIgnoreCase)
                && !normalizedPath.EndsWith(@"\init.bnk", StringComparison.OrdinalIgnoreCase)
                && !normalizedPath.EndsWith(@"\animation_blood_data.bnk", StringComparison.OrdinalIgnoreCase);
        }

        private byte[] ReadData(PackFile bnk, string bnkPath, long offset, int length)
        {
            var source = bnk.DataSource;
            var decodedSize = GetDecodedSize(source);
            if (offset < 0 || length < 0 || offset > decodedSize || length > decodedSize - offset)
                throw new InvalidDataException($"The requested BNK range ({offset:N0} + {length:N0}) exceeds the decoded file size ({decodedSize:N0}).");

            if (TryGetDirectReadLocation(bnk, bnkPath, out var filePath, out var fileOffset))
                return ReadFileRange(filePath, checked(fileOffset + offset), length);

            return ReadByteArrayRange(source.ReadData(), offset, length);
        }

        private bool TryGetDirectReadLocation(PackFile bnk, string bnkPath, out string filePath, out long fileOffset)
        {
            if (bnk.DataSource is PackedFileSource { IsCompressed: false, IsEncrypted: false } packedSource)
            {
                filePath = packedSource.Parent.FilePath;
                fileOffset = packedSource.Offset;
                return true;
            }

            if (bnk.DataSource is FileSystemSource)
            {
                var container = _packFileService.GetPackFileContainer(bnk);
                if (container?.ContainerType == PackFileContainerType.SystemFolder && !string.IsNullOrWhiteSpace(container.SystemFilePath))
                {
                    var systemFilePath = Path.Combine(container.SystemFilePath, bnkPath);
                    if (File.Exists(systemFilePath))
                    {
                        filePath = systemFilePath;
                        fileOffset = 0;
                        return true;
                    }
                }
            }

            filePath = "";
            fileOffset = 0;
            return false;
        }

        private static byte[] ReadByteArrayRange(byte[] data, long offset, int length)
        {
            var result = new byte[length];
            Array.Copy(data, offset, result, 0, length);
            return result;
        }

        private static long GetDecodedSize(IDataSource source)
        {
            return source is PackedFileSource { IsCompressed: true } packedSource ? packedSource.UncompressedSize : source.Size;
        }

        private static byte[] ReadFileRange(string filePath, long offset, int length)
        {
            var result = new byte[length];
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(offset, SeekOrigin.Begin);
            stream.ReadExactly(result);
            return result;
        }

        private void PrintHircList(List<HircItem> hircItems, string header)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"\n Result: {header}");
            var unknownHircs = hircItems.Count(hircItem => hircItem is UnknownHircItem);
            var errorHircs = hircItems.Count(hircItem => hircItem.HasError);
            stringBuilder.AppendLine($"\t Total Hirc Items: {hircItems.Count} Unknown: {unknownHircs} Decoding Errors:{errorHircs}");

            var groupedHircs = hircItems.GroupBy(hircItem => hircItem.HircType);
            var groupedWithErrors = groupedHircs.Where(group => group.Any(hircItem => hircItem is UnknownHircItem || hircItem.HasError));
            var groupedWithoutErrors = groupedHircs.Where(group => group.Any(hircItem => hircItem is not UnknownHircItem && !hircItem.HasError));

            stringBuilder.AppendLine("\t\t Succeeded:");
            foreach (var group in groupedWithoutErrors)
                stringBuilder.AppendLine($"\t\t\t {group.Key}: Count: {group.Count()}");

            if (groupedWithErrors.Any())
            {
                stringBuilder.AppendLine("\t\t Failed:");
                foreach (var group in groupedWithErrors)
                {
                    var errorCount = group.Count(hircItem => hircItem is UnknownHircItem || hircItem.HasError);
                    stringBuilder.AppendLine($"\t\t\t {group.Key}: {errorCount}/{group.Count()} Failed");
                }
            }

            _logger.Here().Information(stringBuilder.ToString());
        }
    }
}
