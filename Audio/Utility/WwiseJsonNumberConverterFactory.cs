using Audio.Storage;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Audio.Utility
{
    public class WwiseJsonNumberConverterFactory : JsonConverterFactory
    {
        private readonly IAudioRepository _audioRepository;

        public WwiseJsonNumberConverterFactory(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == typeof(uint))
                return true;
            return false;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new WwiseJsonNumberConverter(_audioRepository);
        }
    }

    public class WwiseJsonNumberConverter : JsonConverter<uint>
    {
        private readonly IAudioRepository _audioRepository;

        public WwiseJsonNumberConverter(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == typeof(uint))
                return true;
            return false;
        }

        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            var name = _audioRepository.GetNameFromHash(value, out bool found);
            if (found)
                writer.WriteStringValue($"{name}[{value}]");
            else
                writer.WriteStringValue($"{value}");
        }
    }
}
