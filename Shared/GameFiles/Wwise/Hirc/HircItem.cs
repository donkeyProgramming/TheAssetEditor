using Shared.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc
{
    public abstract class HircItem
    {
        readonly ILogger _logger = Logging.Create<HircItem>();

        public string BnkFilePath { get; set; } = "Not Set";
        public bool IsCA { get; set; }
        public uint LanguageId { get; set; } 
        public uint ByteIndexInFile { get; set; }
        public uint IndexInFile { get; set; }
        public bool HasError { get; set; } = true;
        public bool IsTarget { get; set; }
        public List<HircItem>? HircChildren { get; set; }
        public HircHeader Header { get; set; } = new HircHeader();
        public AkBkHircType HircType { get => Header.HircType; set => Header.HircType = value; }
        public uint SectionSize { get => Header.SectionSize; set => Header.SectionSize = value; }
        public uint Id { get => Header.Id; set => Header.Id = value; }

        public static HircItem ReadData(
            string filePath,
            ByteChunk chunk,
            uint bankGeneratorVersion,
            uint languageId,
            bool isCA,
            uint itemIndex,
            int? expectedLength = null)
        {
            if (expectedLength.HasValue && expectedLength.Value < HircHeader.Size)
                throw new InvalidDataException($"HIRC item {itemIndex} is only {expectedLength.Value} bytes.");

            var itemStartIndex = chunk.Index;
            var hircType = (AkBkHircType)chunk.PeakByte();
            var factory = HircFactory.CreateFactory(bankGeneratorVersion);
            HircItem hircItem;

            try
            {
                hircItem = factory.CreateInstance(hircType);
                hircItem.IndexInFile = itemIndex;
                hircItem.ByteIndexInFile = itemIndex;
                hircItem.BnkFilePath = filePath;
                hircItem.LanguageId = languageId;
                hircItem.IsCA = isCA;
                hircItem.ReadHirc(chunk);
            }
            catch (Exception exception)
            {
                chunk.Index = itemStartIndex;

                hircItem = new UnknownHircItem
                {
                    ErrorMsg = exception.Message,
                    ByteIndexInFile = itemIndex,
                    BnkFilePath = filePath
                };
                hircItem.ReadHirc(chunk);
            }

            var bytesRead = chunk.Index - itemStartIndex;
            if (expectedLength.HasValue && bytesRead != expectedLength.Value)
                throw new InvalidDataException($"HIRC item {itemIndex} expected {expectedLength.Value} bytes but read {bytesRead}.");

            return hircItem;
        }

        public void ReadHirc(ByteChunk chunk)
        {
            try
            {
                var indexBeforeRead = chunk.Index;
                ByteIndexInFile = (uint)indexBeforeRead;

                Header = HircHeader.ReadData(chunk);
                ReadData(chunk);

                var currentIndex = chunk.Index;
                var indexAfterRead = (int)(indexBeforeRead + HircHeader.PrefixSize + SectionSize);
                chunk.Index = indexAfterRead;
                HasError = false;
            }

            catch (Exception e)
            {
                _logger.Here().Error($"Failed to parse object {Id} of type {HircType} in {BnkFilePath} at index {IndexInFile} - " + e.Message);
                throw;
            }
        }

        protected MemoryStream WriteHeader()
        {
            var memStream = new MemoryStream();
            memStream.Write(HircHeader.WriteData(Header));
            return memStream;
        }

        protected abstract void ReadData(ByteChunk chunk);
        public abstract byte[] WriteData();
        public abstract void UpdateSectionSize(); 
    }
}
