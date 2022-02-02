using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileTypes.ByteParsing
{
    public class IOFunctions
    {
        // static public byte[] staticBuffer = new byte[1024];
        public static string ReadZeroTerminatedAscii(BinaryReader reader, byte[] preAllocatedBuffer)
        {
            var index = 0;
            byte ch2 = reader.ReadByte();
            while (ch2 != 0)
            {
                preAllocatedBuffer[index++] = ch2;
                ch2 = reader.ReadByte();
            }

            return Encoding.UTF8.GetString(preAllocatedBuffer, 0, index);
        }
    }
}
