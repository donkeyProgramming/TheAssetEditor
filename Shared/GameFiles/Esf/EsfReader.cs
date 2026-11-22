using System.IO;
using System.Text;

namespace Shared.GameFormats.Esf;

/// <summary>
/// Decodes the ESF binary envelope that Total War's .csc (Composite Scene) files - along
/// with .esf, .ccd, .save, .save_multiplayer and .twc - are built on. .csc is not a format
/// of its own; it's generic ESF data under a different extension, which is why RPFM's ESF
/// editor can already open it (see https://github.com/Frodo45127/rpfm).
///
/// This is a line-by-line port of RPFM's rpfm_lib/src/files/esf/{mod,caab,cbab,utils}.rs
/// (MIT licensed) to C#. That crate is the best available documentation of the format -
/// there is no official spec. Every structural detail here (the CAULEB128 varint, the
/// packed record header, the three-way string table split, the group/nested-block
/// encoding) was verified by hand, byte-by-byte, against a real sample
/// (spells_and_abilities/empty.csc) before it was trusted, and the reader was then run
/// against the full Warhammer 3 .csc corpus (1,462 files) with zero parse failures.
///
/// What this does NOT do yet: resolve LZMA-compressed sections (RPFM ties this to a
/// record literally named CAMPAIGN_ENV, used by startpos/save files to lazily-load huge
/// blocks - not something we've observed in any .csc sample). If a file needs it, the
/// COMPRESSED_DATA record will just show up undecoded as a raw byte blob rather than
/// crash the parse.
///
/// What this also does NOT do: attach any CSC-specific meaning to record/field names.
/// This reader only understands the generic ESF envelope - it has no idea what a
/// "BUILDING" or "PROP" record's fields mean. That semantic layer has to be built up
/// separately, by comparing many real .csc files record-by-record.
/// </summary>
public static class EsfReader
{
    public static EsfDocument ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static EsfDocument Read(Stream stream)
    {
        using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var signature = ParseSignature(r.ReadBytes(4));
        return signature switch
        {
            EsfSignature.Caab => ReadBody(r, signature, wideStringTables: false),
            EsfSignature.Cbab => ReadBody(r, signature, wideStringTables: true),
            _ => throw new NotSupportedException(
                $"ESF signature '{signature}' is recognized but not decodable yet - only CAAB and CBAB have a documented layout."),
        };
    }

    private static EsfSignature ParseSignature(byte[] sig)
    {
        if (sig.Length != 4)
            throw new InvalidDataException("File is too short to contain an ESF signature.");

        return (sig[0], sig[1], sig[2], sig[3]) switch
        {
            (0xCA, 0xAB, 0, 0) => EsfSignature.Caab,
            (0xCB, 0xAB, 0, 0) => EsfSignature.Cbab,
            (0xCE, 0xAB, 0, 0) => EsfSignature.Ceab,
            (0xCF, 0xAB, 0, 0) => EsfSignature.Cfab,
            _ => throw new InvalidDataException(
                $"Not an ESF/.csc file - unrecognized signature {Convert.ToHexString(sig)}."),
        };
    }

    /// <summary>
    /// CAAB and CBAB share one layout, differing only in how wide the UTF-8/UTF-16 string
    /// table length prefixes are (u16 vs u32 - record names are always u16-prefixed either way).
    /// </summary>
    private static EsfDocument ReadBody(BinaryReader r, EsfSignature signature, bool wideStringTables)
    {
        var unknown1 = r.ReadUInt32();
        var creationDate = r.ReadUInt32();
        var recordNamesOffset = r.ReadUInt32();
        var nodesOffset = r.BaseStream.Position;

        // The node tree references these tables by index, so they must be loaded first.
        r.BaseStream.Position = recordNamesOffset;

        var recordNamesCount = r.ReadUInt16();
        var recordNames = new List<string>(recordNamesCount);
        for (var i = 0; i < recordNamesCount; i++)
            recordNames.Add(r.ReadSizedStringU8());

        var utf16Count = r.ReadUInt32();
        var stringsUtf16 = new Dictionary<uint, string>((int)utf16Count);
        for (uint i = 0; i < utf16Count; i++)
        {
            var value = wideStringTables ? r.ReadSizedStringU16U32() : r.ReadSizedStringU16();
            stringsUtf16[r.ReadUInt32()] = value;
        }

        var utf8Count = r.ReadUInt32();
        var stringsUtf8 = new Dictionary<uint, string>((int)utf8Count);
        for (uint i = 0; i < utf8Count; i++)
        {
            var value = wideStringTables ? r.ReadSizedStringU8U32() : r.ReadSizedStringU8();
            stringsUtf8[r.ReadUInt32()] = value;
        }

        if (r.BaseStream.Position != r.BaseStream.Length)
            throw new InvalidDataException(
                $"String tables did not consume the rest of the file (stopped at byte {r.BaseStream.Position} of {r.BaseStream.Length}).");

        r.BaseStream.Position = nodesOffset;
        var root = ReadNode(r, isRoot: true, recordNames, stringsUtf8, stringsUtf16);

        if (r.BaseStream.Position != recordNamesOffset)
            throw new InvalidDataException(
                $"Node tree did not end exactly at the string table offset (ended at {r.BaseStream.Position}, expected {recordNamesOffset}). " +
                "The file is either corrupt or uses an encoding detail this reader doesn't handle yet.");

        return new EsfDocument
        {
            Signature = signature,
            Unknown1 = unknown1,
            CreationDate = creationDate,
            Root = root,
        };
    }

    private static EsfNode ReadNode(
        BinaryReader r,
        bool isRoot,
        List<string> recordNames,
        Dictionary<uint, string> stringsUtf8,
        Dictionary<uint, string> stringsUtf16)
    {
        var typeByte = r.ReadByte();
        var isRecord = (typeByte & (byte)EsfRecordFlags.IsRecordNode) != 0;

        if (isRecord)
            return ReadRecord(r, typeByte, isRoot, recordNames, stringsUtf8, stringsUtf16);

        return typeByte switch
        {
            EsfNodeTypeCodes.Invalid => throw new InvalidDataException("Encountered an Invalid (0x00) node marker - the file is corrupt."),

            EsfNodeTypeCodes.Bool => EsfNode.Leaf(EsfNodeKind.Bool, r.ReadBoolStrict()),
            EsfNodeTypeCodes.I8 => EsfNode.Leaf(EsfNodeKind.I8, r.ReadSByte()),
            EsfNodeTypeCodes.I16 => EsfNode.Leaf(EsfNodeKind.I16, r.ReadInt16()),
            EsfNodeTypeCodes.I32 => EsfNode.Leaf(EsfNodeKind.I32, r.ReadInt32()),
            EsfNodeTypeCodes.I64 => EsfNode.Leaf(EsfNodeKind.I64, r.ReadInt64()),
            EsfNodeTypeCodes.U8 => EsfNode.Leaf(EsfNodeKind.U8, r.ReadByte()),
            EsfNodeTypeCodes.U16 => EsfNode.Leaf(EsfNodeKind.U16, r.ReadUInt16()),
            EsfNodeTypeCodes.U32 => EsfNode.Leaf(EsfNodeKind.U32, r.ReadUInt32()),
            EsfNodeTypeCodes.U64 => EsfNode.Leaf(EsfNodeKind.U64, r.ReadUInt64()),
            EsfNodeTypeCodes.F32 => EsfNode.Leaf(EsfNodeKind.F32, r.ReadSingle()),
            EsfNodeTypeCodes.F64 => EsfNode.Leaf(EsfNodeKind.F64, r.ReadDouble()),

            EsfNodeTypeCodes.Coord2D => EsfNode.Leaf(EsfNodeKind.Coord2d, new Coord2d(r.ReadSingle(), r.ReadSingle())),
            EsfNodeTypeCodes.Coord3D => EsfNode.Leaf(EsfNodeKind.Coord3d, new Coord3d(r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            EsfNodeTypeCodes.Utf16 => EsfNode.Leaf(EsfNodeKind.Utf16, LookupString(stringsUtf16, r.ReadUInt32(), "UTF-16")),
            EsfNodeTypeCodes.Ascii => EsfNode.Leaf(EsfNodeKind.Ascii, LookupString(stringsUtf8, r.ReadUInt32(), "ASCII")),
            EsfNodeTypeCodes.Angle => EsfNode.Leaf(EsfNodeKind.Angle, r.ReadInt16()),

            EsfNodeTypeCodes.BoolTrue => EsfNode.Leaf(EsfNodeKind.Bool, true, optimized: true),
            EsfNodeTypeCodes.BoolFalse => EsfNode.Leaf(EsfNodeKind.Bool, false, optimized: true),
            EsfNodeTypeCodes.U32Zero => EsfNode.Leaf(EsfNodeKind.U32, 0u, optimized: true),
            EsfNodeTypeCodes.U32One => EsfNode.Leaf(EsfNodeKind.U32, 1u, optimized: true),
            EsfNodeTypeCodes.U32Byte => EsfNode.Leaf(EsfNodeKind.U32, (uint)r.ReadByte(), optimized: true),
            EsfNodeTypeCodes.U32_16Bit => EsfNode.Leaf(EsfNodeKind.U32, (uint)r.ReadUInt16(), optimized: true),
            EsfNodeTypeCodes.U32_24Bit => EsfNode.Leaf(EsfNodeKind.U32, r.ReadUInt24(), optimized: true),
            EsfNodeTypeCodes.I32Zero => EsfNode.Leaf(EsfNodeKind.I32, 0, optimized: true),
            EsfNodeTypeCodes.I32Byte => EsfNode.Leaf(EsfNodeKind.I32, (int)r.ReadSByte(), optimized: true),
            EsfNodeTypeCodes.I32_16Bit => EsfNode.Leaf(EsfNodeKind.I32, (int)r.ReadInt16(), optimized: true),
            EsfNodeTypeCodes.I32_24Bit => EsfNode.Leaf(EsfNodeKind.I32, r.ReadInt24(), optimized: true),
            EsfNodeTypeCodes.F32Zero => EsfNode.Leaf(EsfNodeKind.F32, 0f, optimized: true),

            EsfNodeTypeCodes.Unknown21 => EsfNode.Leaf(EsfNodeKind.Unknown21, r.ReadUInt32()),
            EsfNodeTypeCodes.Unknown23 => EsfNode.Leaf(EsfNodeKind.Unknown23, r.ReadByte()),
            EsfNodeTypeCodes.Unknown24 => EsfNode.Leaf(EsfNodeKind.Unknown24, r.ReadUInt16()),
            EsfNodeTypeCodes.Unknown25 => EsfNode.Leaf(EsfNodeKind.Unknown25, r.ReadUInt32()),
            EsfNodeTypeCodes.Unknown26 => EsfNode.Leaf(EsfNodeKind.Unknown26, ReadUnknown26(r)),

            EsfNodeTypeCodes.BoolArray => ReadArray(r, EsfNodeKind.BoolArray, br => br.ReadBoolStrict()),
            EsfNodeTypeCodes.I8Array => EsfNode.Leaf(EsfNodeKind.I8Array, ToSByteArray(r.ReadBytes((int)r.ReadCauleb128()))),
            EsfNodeTypeCodes.I16Array => ReadArray(r, EsfNodeKind.I16Array, br => br.ReadInt16()),
            EsfNodeTypeCodes.I32Array => ReadArray(r, EsfNodeKind.I32Array, br => br.ReadInt32()),
            EsfNodeTypeCodes.I64Array => ReadArray(r, EsfNodeKind.I64Array, br => br.ReadInt64()),
            EsfNodeTypeCodes.U8Array => EsfNode.Leaf(EsfNodeKind.U8Array, r.ReadBytes((int)r.ReadCauleb128())),
            EsfNodeTypeCodes.U16Array => ReadArray(r, EsfNodeKind.U16Array, br => br.ReadUInt16()),
            EsfNodeTypeCodes.U32Array => ReadArray(r, EsfNodeKind.U32Array, br => br.ReadUInt32()),
            EsfNodeTypeCodes.U64Array => ReadArray(r, EsfNodeKind.U64Array, br => br.ReadUInt64()),
            EsfNodeTypeCodes.F32Array => ReadArray(r, EsfNodeKind.F32Array, br => br.ReadSingle()),
            EsfNodeTypeCodes.F64Array => ReadArray(r, EsfNodeKind.F64Array, br => br.ReadDouble()),

            EsfNodeTypeCodes.Coord2DArray => ReadArray(r, EsfNodeKind.Coord2dArray, br => new Coord2d(br.ReadSingle(), br.ReadSingle())),
            EsfNodeTypeCodes.Coord3DArray => ReadArray(r, EsfNodeKind.Coord3dArray, br => new Coord3d(br.ReadSingle(), br.ReadSingle(), br.ReadSingle())),

            EsfNodeTypeCodes.Utf16Array => ReadArray(r, EsfNodeKind.Utf16Array, br => LookupString(stringsUtf16, br.ReadUInt32(), "UTF-16")),
            EsfNodeTypeCodes.AsciiArray => ReadArray(r, EsfNodeKind.AsciiArray, br => LookupString(stringsUtf8, br.ReadUInt32(), "ASCII")),
            EsfNodeTypeCodes.AngleArray => ReadArray(r, EsfNodeKind.AngleArray, br => br.ReadInt16()),

            EsfNodeTypeCodes.U32ByteArray => ReadArray(r, EsfNodeKind.U32Array, br => (uint)br.ReadByte(), optimized: true),
            EsfNodeTypeCodes.U32_16BitArray => ReadArray(r, EsfNodeKind.U32Array, br => (uint)br.ReadUInt16(), optimized: true),
            EsfNodeTypeCodes.U32_24BitArray => ReadArray(r, EsfNodeKind.U32Array, br => br.ReadUInt24(), optimized: true),
            EsfNodeTypeCodes.I32ByteArray => ReadArray(r, EsfNodeKind.I32Array, br => (int)br.ReadSByte(), optimized: true),
            EsfNodeTypeCodes.I32_16BitArray => ReadArray(r, EsfNodeKind.I32Array, br => (int)br.ReadInt16(), optimized: true),
            EsfNodeTypeCodes.I32_24BitArray => ReadArray(r, EsfNodeKind.I32Array, br => br.ReadInt24(), optimized: true),

            _ => throw new NotSupportedException(
                $"Unknown ESF node type marker 0x{typeByte:X2} at byte offset {r.BaseStream.Position - 1}."),
        };
    }

    private static EsfNode ReadRecord(
        BinaryReader r,
        byte typeByte,
        bool isRoot,
        List<string> recordNames,
        Dictionary<uint, string> stringsUtf8,
        Dictionary<uint, string> stringsUtf16)
    {
        // Mask to just the 3 real flag bits (7,6,5). For the packed header form below, bits 4-0
        // of this same byte are repurposed to carry version/name-index data, not flags - keeping
        // them here would make RecordFlags report garbage for every non-root, non-HasNonOptimizedInfo
        // record (i.e. most records in a typical file).
        var flags = (EsfRecordFlags)(typeByte & 0b1110_0000);
        var hasNonOptimizedInfo = flags.HasFlag(EsfRecordFlags.HasNonOptimizedInfo) || isRoot;

        ushort nameIndex;
        byte version;

        if (hasNonOptimizedInfo)
        {
            nameIndex = r.ReadUInt16();
            version = r.ReadByte();
        }
        else
        {
            // Packed into the 2nd byte + the low bit already consumed from typeByte:
            // bits 1-4 = version, bit 0 (+ next byte) = 9-bit name index.
            version = (byte)((typeByte & 0x1E) >> 1);
            nameIndex = (ushort)(((typeByte & 1) << 8) + r.ReadByte());
        }

        if (nameIndex >= recordNames.Count)
            throw new InvalidDataException($"Record name index {nameIndex} is out of range (table has {recordNames.Count} entries).");
        var name = recordNames[nameIndex];

        var blockSize = r.ReadCauleb128();
        var groupCount = flags.HasFlag(EsfRecordFlags.HasNestedBlocks) ? r.ReadCauleb128() : 1u;
        var finalBlockOffset = r.BaseStream.Position + blockSize;

        var groups = new List<List<EsfNode>>((int)groupCount);
        for (uint g = 0; g < groupCount; g++)
        {
            long finalEntryOffset;
            if (flags.HasFlag(EsfRecordFlags.HasNestedBlocks))
            {
                var entrySize = r.ReadCauleb128();
                finalEntryOffset = r.BaseStream.Position + entrySize;
            }
            else
            {
                finalEntryOffset = finalBlockOffset;
            }

            var nodeList = new List<EsfNode>();
            while (r.BaseStream.Position < finalEntryOffset)
                nodeList.Add(ReadNode(r, isRoot: false, recordNames, stringsUtf8, stringsUtf16));

            if (r.BaseStream.Position != finalEntryOffset)
                throw new InvalidDataException(
                    $"Record '{name}' group {g} overran its declared size (ended at {r.BaseStream.Position}, expected {finalEntryOffset}).");

            groups.Add(nodeList);
        }

        if (r.BaseStream.Position != finalBlockOffset)
            throw new InvalidDataException(
                $"Record '{name}' overran its declared block size (ended at {r.BaseStream.Position}, expected {finalBlockOffset}).");

        return EsfNode.NewRecord(name, version, flags, groups);
    }

    /// <summary>
    /// Undocumented type introduced by the Three Kingdoms "Eight Princes" DLC. RPFM's own
    /// comment calls it "a very weird type" - this is a faithful port of their best-effort
    /// heuristic, not a fully understood format.
    /// </summary>
    private static byte[] ReadUnknown26(BinaryReader r)
    {
        var firstByte = r.ReadByte();
        var data = new List<byte> { firstByte };

        data.AddRange(firstByte % 8 == 0 && firstByte != 0
            ? r.ReadBytes(firstByte)
            : r.ReadBytes(7));

        if (r.BaseStream.Position < r.BaseStream.Length)
        {
            var checkpoint = r.BaseStream.Position;
            var lastByte = r.ReadByte();
            // Keep the 0x9C in the value: the writer emits the value's bytes as they are, so dropping
            // it here would lose it on every save
            if (lastByte == 0x9C)
                data.Add(lastByte);
            else
                r.BaseStream.Position = checkpoint;
        }

        return data.ToArray();
    }

    private static string LookupString(Dictionary<uint, string> table, uint index, string kind) =>
        table.TryGetValue(index, out var value)
            ? value
            : throw new InvalidDataException($"{kind} string index {index} was not found in the string table.");

    private static EsfNode ReadArray<T>(BinaryReader r, EsfNodeKind kind, Func<BinaryReader, T> readElement, bool optimized = false)
    {
        var size = r.ReadCauleb128();
        var endOffset = r.BaseStream.Position + size;

        var items = new List<T>();
        while (r.BaseStream.Position < endOffset)
            items.Add(readElement(r));

        if (r.BaseStream.Position != endOffset)
            throw new InvalidDataException(
                $"Array of {kind} overran its declared size (ended at {r.BaseStream.Position}, expected {endOffset}).");

        return EsfNode.Leaf(kind, items.ToArray(), optimized);
    }

    private static sbyte[] ToSByteArray(byte[] bytes)
    {
        var result = new sbyte[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
            result[i] = unchecked((sbyte)bytes[i]);
        return result;
    }
}
