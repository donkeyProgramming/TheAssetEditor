using Serilog;
using Shared.Core.ByteParsing;
using Shared.Core.ErrorHandling;
using Shared.Core.PackFiles.Models;

namespace Shared.Core.PackFiles
{
    public static class PackFileVersionConverter
    {
        static List<(PackFileVersion EnumValue, string StringValue)> _values = new List<(PackFileVersion EnumValue, string StringValue)>()
        {
            (PackFileVersion.PFH0,  "PFH0"),
            (PackFileVersion.PFH2,  "PFH2"),
            (PackFileVersion.PFH3,  "PFH3"),
            (PackFileVersion.PFH4,  "PFH4"),
            (PackFileVersion.PFH5,  "PFH5"),
            (PackFileVersion.PFH6,  "PFH6"),
        };

        public static string ToString(PackFileVersion versionEnum) => _values.First(x => x.EnumValue == versionEnum).StringValue;
        public static PackFileVersion GetEnum(string versionStr) => _values.First(x => x.StringValue == versionStr.ToUpper()).EnumValue;
    }

    public static class PackFileSerializer
    {
        static readonly ILogger _logger = Logging.CreateStatic(typeof(PackFileSerializer));

        public static PackFileContainer Load(string packFileSystemPath, BinaryReader reader, bool loadWemFiles, IDuplicatePackFileResolver dubplicatePackFileResolver)
        {
            try
            {
                var fileNameBuffer = new byte[1024];
                var name = Path.GetFileNameWithoutExtension(packFileSystemPath);
                var output = new PackFileContainer(name)
                {
                    SystemFilePath = packFileSystemPath,
                    Name = Path.GetFileNameWithoutExtension(packFileSystemPath),
                    Header = ReadHeader(reader),
                    OriginalLoadByteSize = new FileInfo(packFileSystemPath).Length,
                };

                // If larger then int.max throw error
                if (output.Header.FileCount > int.MaxValue)
                    throw new Exception("Too many files in packfile!");

                output.FileList = new Dictionary<string, PackFile>((int)output.Header.FileCount);

                var packedFileSourceParent = new PackedFileSourceParent()
                {
                    FilePath = packFileSystemPath,
                };

                var offset = output.Header.DataStart;
                var headerVersion = output.Header.Version;
                for (var i = 0; i < output.Header.FileCount; i++)
                {
                    var size = reader.ReadUInt32();

                    if (output.Header.HasIndexWithTimeStamp)
                        reader.ReadUInt32();

                    byte isCompressed = 0;
                    if (headerVersion == PackFileVersion.PFH5)
                        isCompressed = reader.ReadByte();   // Is the file actually compressed, or is it just a compressed format?

                    var fullPackedFileName = IOFunctions.ReadZeroTerminatedAscii(reader, fileNameBuffer).ToLower();

                    var packFileName = Path.GetFileName(fullPackedFileName);
                    var fileContent = new PackFile(packFileName, new PackedFileSource(packedFileSourceParent, offset, size));

                    // Should we skip sound files? 
                    var addFile = true;
                    if (loadWemFiles == false)
                        addFile = packFileName.EndsWith(".wem", StringComparison.InvariantCultureIgnoreCase) == false;

                    if (addFile)
                    {
                        if (dubplicatePackFileResolver.CheckForDuplicates)
                        {
                            var containsKey = output.FileList.ContainsKey(fullPackedFileName);
                            if (containsKey)
                            {
                                if (dubplicatePackFileResolver.KeepDuplicateFile(fullPackedFileName))
                                {
                                    _logger.Here().Warning($"Duplicate file found {fullPackedFileName}");
                                    output.FileList.Add(fullPackedFileName + Guid.NewGuid().ToString(), fileContent);
                                }
                            }
                            else
                            {
                                output.FileList.Add(fullPackedFileName, fileContent);
                            }
                        }
                        else
                        {
                            output.FileList.Add(fullPackedFileName, fileContent);
                        }

                    }
                    offset += size;
                }

                return output;
            }
            catch (Exception e)
            {
                _logger.Here().Error($"Failed to load packfile {packFileSystemPath} - {e.Message}");
                throw;
            }
        }

        static PFHeader ReadHeader(BinaryReader reader)
        {
            var fileNameBuffer = new byte[1024];
            var header = new PFHeader()
            {
                _strVersion = new string(reader.ReadChars(4)),      // 4
                ByteMask = reader.ReadInt32(),                      // 8
                ReferenceFileCount = reader.ReadUInt32(),           // 12    
            };

            var pack_file_index_size = reader.ReadUInt32();         // 16
            var pack_file_count = reader.ReadUInt32();              // 20
            var packed_file_index_size = reader.ReadUInt32();       // 24

            // Read the buffer of data stuff
            if (header.Version == PackFileVersion.PFH0)
            {
                header.Buffer = new byte[0];
            }
            else if (header.Version == PackFileVersion.PFH2 || header.Version == PackFileVersion.PFH3)
            {
                header.Buffer = reader.ReadBytes(8);
                // Uint64 timestamp
            }
            else if (header.Version == PackFileVersion.PFH4 || header.Version == PackFileVersion.PFH5)
            {
                if (header.HasExtendedHeader)
                    header.Buffer = reader.ReadBytes(24);
                else
                    header.Buffer = reader.ReadBytes(4);

                // Uint32 timestamp
                // output.HasExtendedHeader 20 bytes missing? Used by Arena, we dont care 
            }
            else if (header.Version == PackFileVersion.PFH6)
            {
                header.Buffer = reader.ReadBytes(284);

                // game_version u32
                // build_number u32
                // authoring_tool char 44
                // extra_subheader_data u32, not used 
            }
            else
            {
                throw new Exception($"Unknown packfile type {header.PackFileType}");
            }

            for (var i = 0; i < header.ReferenceFileCount; i++)
                header.DependantFiles.Add(IOFunctions.ReadZeroTerminatedAscii(reader, fileNameBuffer));

            header.DataStart = 24 + (uint)header.Buffer.Length + pack_file_index_size + packed_file_index_size;
            header.FileCount = pack_file_count;

            return header;
        }


        public static void WriteHeader(PFHeader header, uint fileContentSize, BinaryWriter writer)
        {
            var packFileTypeStr = PackFileVersionConverter.ToString(header.Version);        // 4
            foreach (var c in packFileTypeStr)
                writer.Write(c);

            writer.Write(header.ByteMask);                                                  // 8
            writer.Write(header.DependantFiles.Count);                                      // 12

            var pack_file_index_size = 0;
            foreach (var file in header.DependantFiles)
                pack_file_index_size += file.Length + 1;

            writer.Write(pack_file_index_size);                                             // 16
            writer.Write(header.FileCount);                                                 // 20
            writer.Write(fileContentSize);                                                  // 24


            switch (header.Version)
            {
                case PackFileVersion.PFH0:
                    break;// Nothing needed to do
                case PackFileVersion.PFH2:
                case PackFileVersion.PFH3:
                    // 64 bit timestamp
                    writer.Write(0);
                    writer.Write(0);
                    break;
                case PackFileVersion.PFH4:
                case PackFileVersion.PFH5:
                    if (header.HasExtendedHeader)
                        throw new Exception("Not supported packfile type");

                    writer.Write(PFHeader.DefaultTimeStamp);
                    break;

                default:
                    throw new Exception("Not supported packfile type");
            }

            foreach (var file in header.DependantFiles)
            {
                foreach (byte c in file)
                    writer.Write(c);
                writer.Write((byte)0);
            }
        }
    }
}
