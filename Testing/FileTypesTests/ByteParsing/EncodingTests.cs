using NUnit.Framework;
using Shared.Core.ByteParsing;

namespace FileTypesTests.ByteParsing
{
    public class EncodingTests
    {
        [Test]
        public void Byte()
        {
            Compare<byte>(ByteParsers.Byte, 32);
            Compare<byte>(ByteParsers.Byte, 0);
            Compare<byte>(ByteParsers.Byte, 200);
            Compare<byte>(ByteParsers.Byte, 97);
            Compare<byte>(ByteParsers.Byte, 6);
        }

        [Test]
        public void Int32()
        {
            Compare<int>(ByteParsers.Int32, 30000002);
            Compare<int>(ByteParsers.Int32, 0);
            Compare<int>(ByteParsers.Int32, 200);
            Compare<int>(ByteParsers.Int32, -97);
            Compare<int>(ByteParsers.Int32, 6);
        }

        [Test]
        public void Int64()
        {
            Compare<long>(ByteParsers.Int64, 32000000000);
            Compare<long>(ByteParsers.Int64, 0);
            Compare<long>(ByteParsers.Int64, 200);
            Compare<long>(ByteParsers.Int64, -97);
            Compare<long>(ByteParsers.Int64, 6);
        }

        [Test]
        public void Uint32()
        {
            Compare<uint>(ByteParsers.UInt32, 32000000);
            Compare<uint>(ByteParsers.UInt32, 0);
            Compare<uint>(ByteParsers.UInt32, 200);
            Compare<uint>(ByteParsers.UInt32, 97);
            Compare<uint>(ByteParsers.UInt32, 6);
        }

        [Test]
        public void Float()
        {
            Compare(ByteParsers.Single, 32);
            Compare(ByteParsers.Single, 0);
            Compare(ByteParsers.Single, -33655.0099f);
            Compare(ByteParsers.Single, 33655.0099f);
            //Compare(ByteParsers.Single, 0.0000000000099f);
        }

        [Test]
        public void Float16()
        {
            Compare(ByteParsers.Float16, 32);
            Compare(ByteParsers.Float16, 0);
            Compare(ByteParsers.Float16, -33655.0099f);
            Compare(ByteParsers.Float16, 33655.0099f);
            Compare(ByteParsers.Float16, 0.0000000000099f);
        }

        [Test]
        public void Short()
        {
            Compare<short>(ByteParsers.Short, 32);
            Compare<short>(ByteParsers.Short, 0);
            Compare<short>(ByteParsers.Short, -3);
            Compare<short>(ByteParsers.Short, 336);
            Compare<short>(ByteParsers.Short, 9871);
        }

        [Test]
        public void UShort()
        {
            Compare<ushort>(ByteParsers.UShort, 32);
            Compare<ushort>(ByteParsers.UShort, 0);
            Compare<ushort>(ByteParsers.UShort, 99);
            Compare<ushort>(ByteParsers.UShort, 336);
            Compare<ushort>(ByteParsers.UShort, 9871);
        }

        [Test]
        public void Bool()
        {
            Compare<bool>(ByteParsers.Bool, true);
            Compare<bool>(ByteParsers.Bool, false);
        }

        [Test]
        public void String()
        {
            Compare(ByteParsers.String, "");
            Compare(ByteParsers.String, "Cats and dogs");
            Compare(ByteParsers.String, "horses");
            Compare(ByteParsers.String, "SHdfxsg...");
        }

        [Test]
        public void OptString()
        {
            Compare(ByteParsers.OptString, "");
            Compare(ByteParsers.OptString, "Cats and dogs");
            Compare(ByteParsers.OptString, "horses");
            Compare(ByteParsers.OptString, "SHdfxsg...");
        }

        [Test]
        public void StringAscii()
        {
            Compare(ByteParsers.StringAscii, "");
            Compare(ByteParsers.StringAscii, "Cats and dogs");
            Compare(ByteParsers.StringAscii, "horses");
            Compare(ByteParsers.StringAscii, "SHdfåxsg...");
        }

        [Test]
        public void OptStringAscii()
        {
            Compare(ByteParsers.OptStringAscii, "");
            Compare(ByteParsers.OptStringAscii, "Cats and dogs");
            Compare(ByteParsers.OptStringAscii, "horses");
            Compare(ByteParsers.OptStringAscii, "SHdfåxsg...");
        }

        public void Compare<T>(SpesificByteParser<T> parser, T value)
        {
            var bytesFromValue = parser.EncodeValue(value, out _);
            parser.TryDecode(bytesFromValue, 0, out var stringValue, out _, out _);

            var encodedFromString = parser.Encode(stringValue, out _);
            parser.TryDecode(bytesFromValue, 0, out var stringValue2, out _, out _);

            CompareBytes(bytesFromValue, encodedFromString);

            Assert.NotNull(stringValue);
            Assert.AreEqual(stringValue, stringValue2);
        }

        public void Compare(StringParser parser, string value)
        {
            var bytesFromValue = parser.EncodeValue(value, out _);
            parser.TryDecode(bytesFromValue, 0, out var stringValue, out _, out _);

            var encodedFromString = parser.Encode(stringValue, out _);
            parser.TryDecode(bytesFromValue, 0, out var stringValue2, out _, out _);

            CompareBytes(bytesFromValue, encodedFromString);

            Assert.NotNull(stringValue);
            Assert.AreEqual(stringValue, stringValue2);
        }

        void CompareBytes(byte[] expected, byte[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }
    }
}
