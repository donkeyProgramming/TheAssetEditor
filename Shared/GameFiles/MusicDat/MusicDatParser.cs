using System.Text;
using Shared.ByteParsing;
using Shared.Core.PackFiles.Models;

namespace Shared.GameFormats.MusicDat
{
    /// <summary>
    /// Reader/writer for campaign_music.dat / battle_music.dat.
    ///
    /// Layout is a version word followed by eight length-prefixed tables, in the
    /// order they appear on <see cref="MusicDatFile"/>. Strings are length-prefixed
    /// UTF-8 with no terminator. Both shipped Warhammer 3 files parse with zero
    /// bytes left over and round-trip byte-identically.
    /// </summary>
    public static class MusicDatParser
    {
        public static MusicDatFile Parse(PackFile packFile) => Parse(packFile.DataSource.ReadData());

        public static MusicDatFile Parse(byte[] bytes)
        {
            var chunk = new ByteChunk(bytes);
            var output = new MusicDatFile { Version = chunk.ReadUInt32() };

            var triggerCount = chunk.ReadUInt32();
            for (var i = 0; i < triggerCount; i++)
                output.Triggers.Add(new MusicDatFile.TriggerEntry { Name = ReadStr32(chunk), InitialValue = chunk.ReadUInt32() });

            var variableCount = chunk.ReadUInt32();
            for (var i = 0; i < variableCount; i++)
                output.Variables.Add(new MusicDatFile.VariableEntry { Name = ReadStr32(chunk), InitialValue = chunk.ReadSingle() });

            var stringVarCount = chunk.ReadUInt32();
            for (var i = 0; i < stringVarCount; i++)
                output.StringVariables.Add(new MusicDatFile.StringVariableEntry { Name = ReadStr32(chunk), InitialValue = ReadStr32(chunk) });

            var stringCount = chunk.ReadUInt32();
            for (var i = 0; i < stringCount; i++)
                output.StringConstants.Add(ReadStr32(chunk));

            var choicePointCount = chunk.ReadUInt32();
            for (var i = 0; i < choicePointCount; i++)
                output.ChoicePoints.Add(new MusicDatFile.ChoicePoint { ChoiceId = chunk.ReadUInt32(), OptionCount = chunk.ReadUInt32() });

            var functionCount = chunk.ReadUInt32();
            for (var i = 0; i < functionCount; i++)
            {
                var fn = new MusicDatFile.FunctionEntry { Name = ReadStr32(chunk), CodeOffset = chunk.ReadUInt32() };
                var paramCount = chunk.ReadUInt32();
                for (var j = 0; j < paramCount; j++)
                    fn.ParameterTypes.Add(chunk.ReadUInt32());
                var localCount = chunk.ReadUInt32();
                for (var j = 0; j < localCount; j++)
                    fn.ReturnOrLocalTypes.Add(chunk.ReadUInt32());
                output.Functions.Add(fn);
            }

            var intrinsicCount = chunk.ReadUInt32();
            for (var i = 0; i < intrinsicCount; i++)
            {
                var it = new MusicDatFile.IntrinsicEntry
                {
                    Name = ReadStr32(chunk),
                    ParameterCount = chunk.ReadUInt32(),
                    Unknown = chunk.ReadUInt32()
                };
                var n = chunk.ReadUInt32();
                for (var j = 0; j < n; j++)
                    it.UnknownArray.Add(chunk.ReadUInt32());
                output.Intrinsics.Add(it);
            }

            var instructionCount = chunk.ReadUInt32();
            for (var i = 0; i < instructionCount; i++)
                output.Instructions.Add(chunk.ReadUInt32());

            if (chunk.BytesLeft != 0)
                throw new InvalidDataException($"MusicDat parse finished with {chunk.BytesLeft} bytes remaining of {bytes.Length}");

            return output;
        }

        public static byte[] Write(MusicDatFile file)
        {
            using var stream = new MemoryStream();

            WriteUInt(stream, file.Version);

            WriteUInt(stream, (uint)file.Triggers.Count);
            foreach (var t in file.Triggers)
            {
                WriteStr32(stream, t.Name);
                WriteUInt(stream, t.InitialValue);
            }

            WriteUInt(stream, (uint)file.Variables.Count);
            foreach (var v in file.Variables)
            {
                WriteStr32(stream, v.Name);
                stream.Write(ByteParsers.Single.EncodeValue(v.InitialValue, out _));
            }

            WriteUInt(stream, (uint)file.StringVariables.Count);
            foreach (var s in file.StringVariables)
            {
                WriteStr32(stream, s.Name);
                WriteStr32(stream, s.InitialValue);
            }

            WriteUInt(stream, (uint)file.StringConstants.Count);
            foreach (var s in file.StringConstants)
                WriteStr32(stream, s);

            WriteUInt(stream, (uint)file.ChoicePoints.Count);
            foreach (var p in file.ChoicePoints)
            {
                WriteUInt(stream, p.ChoiceId);
                WriteUInt(stream, p.OptionCount);
            }

            WriteUInt(stream, (uint)file.Functions.Count);
            foreach (var fn in file.Functions)
            {
                WriteStr32(stream, fn.Name);
                WriteUInt(stream, fn.CodeOffset);
                WriteUInt(stream, (uint)fn.ParameterTypes.Count);
                foreach (var x in fn.ParameterTypes) WriteUInt(stream, x);
                WriteUInt(stream, (uint)fn.ReturnOrLocalTypes.Count);
                foreach (var x in fn.ReturnOrLocalTypes) WriteUInt(stream, x);
            }

            WriteUInt(stream, (uint)file.Intrinsics.Count);
            foreach (var it in file.Intrinsics)
            {
                WriteStr32(stream, it.Name);
                WriteUInt(stream, it.ParameterCount);
                WriteUInt(stream, it.Unknown);
                WriteUInt(stream, (uint)it.UnknownArray.Count);
                foreach (var x in it.UnknownArray) WriteUInt(stream, x);
            }

            WriteUInt(stream, (uint)file.Instructions.Count);
            foreach (var i in file.Instructions)
                WriteUInt(stream, i);

            return stream.ToArray();
        }

        static void WriteUInt(Stream s, uint value) => s.Write(ByteParsers.UInt32.EncodeValue(value, out _));

        static string ReadStr32(ByteChunk chunk)
        {
            var offset = chunk.Index;
            var length = chunk.ReadInt32();

            // A layout change makes some other value land here, so fail clearly rather than
            // trying to allocate whatever it happens to be.
            if (length < 0 || length > chunk.BytesLeft)
                throw new InvalidDataException($"MusicDat string at offset {offset} claims {length} bytes, but only {chunk.BytesLeft} remain");

            return Encoding.UTF8.GetString(chunk.ReadBytes(length));
        }

        static void WriteStr32(Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(ByteParsers.Int32.EncodeValue(bytes.Length, out _));
            stream.Write(bytes);
        }
    }
}
