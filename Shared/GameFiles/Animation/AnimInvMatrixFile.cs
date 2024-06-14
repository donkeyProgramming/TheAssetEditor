using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Animation
{
    public class AnimInvMatrixFile
    {
        public uint Version { get; set; }
        public Matrix[] MatrixList { get; set; }

        public static AnimInvMatrixFile Create(ByteChunk chunk)
        {
            if (chunk.BytesLeft == 0)
                throw new Exception("Trying to load AnimInvMatrix with no data, chunk size = 0");

            chunk.Reset();
            var output = new AnimInvMatrixFile
            {
                Version = chunk.ReadUInt32(),
                MatrixList = new Matrix[chunk.ReadUInt32()]
            };

            for (var i = 0; i < output.MatrixList.Length; i++)
            {
                output.MatrixList[i] = Matrix.Identity;

                output.MatrixList[i].M11 = chunk.ReadSingle();
                output.MatrixList[i].M21 = chunk.ReadSingle();
                output.MatrixList[i].M31 = chunk.ReadSingle();

                output.MatrixList[i].M12 = chunk.ReadSingle();
                output.MatrixList[i].M22 = chunk.ReadSingle();
                output.MatrixList[i].M32 = chunk.ReadSingle();

                output.MatrixList[i].M13 = chunk.ReadSingle();
                output.MatrixList[i].M23 = chunk.ReadSingle();
                output.MatrixList[i].M33 = chunk.ReadSingle();

                output.MatrixList[i].M14 = chunk.ReadSingle();
                output.MatrixList[i].M24 = chunk.ReadSingle();
                output.MatrixList[i].M34 = chunk.ReadSingle();
            }

            var bytesLeft = chunk.BytesLeft;
            if (bytesLeft != 0)
                throw new Exception("Data left in AnimInvMatrix:" + bytesLeft);
            return output;
        }

        /*
		 * 
		 * matrix = matrix.Inverse();
		matrix = matrix.Transpose();

		// run though the inverse bind pose matrix
		for (size_t n = 0; n < 4; n++)
		{
			for (size_t m = 0; m < 3; m++)
			{
				float a = matrix[m][n];
				//float a = 0.1; // TODO: DEBUGGIN CODE, makes incorrect matrices on purpose
				inv_file.write((char*)&a, 4);
			}
		}		
		 */

        public byte[] GetBytes()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(Version);
                    writer.Write(MatrixList.Length);

                    for (var i = 0; i < MatrixList.Length; i++)
                    {
                        writer.Write(MatrixList[i].M11);
                        writer.Write(MatrixList[i].M21);
                        writer.Write(MatrixList[i].M31);

                        writer.Write(MatrixList[i].M12);
                        writer.Write(MatrixList[i].M22);
                        writer.Write(MatrixList[i].M32);

                        writer.Write(MatrixList[i].M13);
                        writer.Write(MatrixList[i].M23);
                        writer.Write(MatrixList[i].M33);

                        writer.Write(MatrixList[i].M14);
                        writer.Write(MatrixList[i].M24);
                        writer.Write(MatrixList[i].M34);
                    }

                    var bytes = memoryStream.ToArray();
                    var temp = Create(new ByteChunk(bytes));
                    return bytes;
                }
            }
        }
    }



    /*
     bool InvMatsParser::Read(const string& path)
{
	ifstream file(path.c_str(), ios::in | ios::binary);
	if (file.is_open())
	{
		DirectX::XMFLOAT4X4 matrix;
		uint32_t bonesCount;

		file.seekg(4, ios::beg);
		file.read(reinterpret_cast<char *>(&bonesCount), sizeof(bonesCount));

		for (size_t mat = 0; mat < bonesCount; ++mat)
		{
			file.read(reinterpret_cast<char *>(&matrix._11), sizeof(matrix._11));
			file.read(reinterpret_cast<char *>(&matrix._31), sizeof(matrix._31));
			file.read(reinterpret_cast<char *>(&matrix._21), sizeof(matrix._21));
			matrix._41 = 0.0f;

			file.read(reinterpret_cast<char *>(&matrix._12), sizeof(matrix._12));
			file.read(reinterpret_cast<char *>(&matrix._32), sizeof(matrix._32));
			file.read(reinterpret_cast<char *>(&matrix._22), sizeof(matrix._22));
			matrix._42 = 0.0f;

			file.read(reinterpret_cast<char *>(&matrix._13), sizeof(matrix._13));
			file.read(reinterpret_cast<char *>(&matrix._33), sizeof(matrix._33));
			file.read(reinterpret_cast<char *>(&matrix._23), sizeof(matrix._23));
			matrix._43 = 0.0f;

			file.read(reinterpret_cast<char *>(&matrix._14), sizeof(matrix._14));
			file.read(reinterpret_cast<char *>(&matrix._34), sizeof(matrix._34));
			file.read(reinterpret_cast<char *>(&matrix._24), sizeof(matrix._24));
			matrix._44 = 1.0f;
			
			mMatricesArray.push_back(move(matrix));
		}
	}
	else
		return false;

	file.close();
	return true;
}
     */
}
