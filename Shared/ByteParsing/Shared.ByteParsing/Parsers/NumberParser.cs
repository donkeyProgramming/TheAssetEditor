namespace Shared.ByteParsing.Parsers
{
    public abstract class NumberParser<T> : SpesificByteParser<T>
    {
        protected abstract int FieldSize { get; }
        public abstract DbTypesEnum Type { get; }

        public abstract string TypeName { get; }

        protected abstract T Decode(byte[] buffer, int index);
        public abstract byte[]? EncodeValue(T value, out string? error);
        public abstract byte[]? Encode(string value, out string? error);

        public virtual bool CanDecode(byte[] buffer, int index, out int bytesRead, out string? _error)
        {
            var bytesLeft = buffer.Length - index;
            if (bytesLeft < FieldSize)
            {
                bytesRead = 0;
                _error = $"Not enough space in stream. Asking for {FieldSize} bytes, but only {bytesLeft} bytes left";
                return false;
            }
            bytesRead = FieldSize;
            _error = null;
            return true;
        }

        public virtual bool TryDecode(byte[] buffer, int index, out string value, out int bytesRead, out string? error)
        {
            var result = TryDecodeValue(buffer, index, out var temp, out bytesRead, out error);
            value = temp?.ToString();
            return result;
        }

        public virtual bool TryDecodeValue(byte[] buffer, int index, out T value, out int bytesRead, out string? error)
        {
            value = default;
            var canDecode = CanDecode(buffer, index, out bytesRead, out error);
            if (canDecode)
                value = Decode(buffer, index);
            return canDecode;
        }

        public object GetValueAsObject(byte[] buffer, int index, out int bytesRead)
        {
            var canDecode = CanDecode(buffer, index, out bytesRead, out var error);
            if (canDecode == false)
                throw new Exception(error);

            var value = Decode(buffer, index) as object;
            return value;
        }
    }
}
