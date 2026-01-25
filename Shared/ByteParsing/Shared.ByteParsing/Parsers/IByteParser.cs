namespace Shared.ByteParsing.Parsers
{
    public enum DbTypesEnum
    {
        Byte,
        String,
        String_ascii,
        FixedString,
        FixedStringAcii,
        Optstring,
        Optstring_ascii,
        Int64,
        Integer,
        uint32,
        Short,
        UShort,
        Single,
        Float16,
        Boolean,
        StringLookup,
        List,
        Vector3,
        Vector4
    }

    public interface IByteParser
    {
        string TypeName { get; }
        DbTypesEnum Type { get; }
        bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error);
        bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? error);
        byte[]? Encode(string value, out string? error);
        object GetValueAsObject(byte[] buffer, int index, out int bytesRead);
    }

    public interface SpesificByteParser<T> : IByteParser
    {
        bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string? error);
        byte[]? EncodeValue(T value, out string? error);
    }
}
