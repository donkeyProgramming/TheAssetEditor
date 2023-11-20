#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\HelperUtils\FBXUnitHelper.h"

namespace wrapdll{

	class PackedVertexCreator
	{
	public:
		/// <summary>
		/// Converts FBX data structure to the common (packed) vertex format
		/// </summary>
		/// <param name="vControlPoint">Vertex Position</param>
		/// <param name="vNormalVector">Normal Vector</param>
		/// <param name="UVmap1">UV coords</param>		
		/// <param name="scaleFactor"></param>
		/// <returns></returns>
		static PackedCommonVertex MakePackedVertex(
			const FbxVector4& vControlPoint,
			const FbxVector4& vNormalVector,
			const FbxVector2& UVmap1,			
			double scaleFactor)
		{
			PackedCommonVertex outVertex;

			outVertex.position.x = static_cast<float>(vControlPoint.mData[0] * scaleFactor);
			outVertex.position.y = static_cast<float>(vControlPoint.mData[1] * scaleFactor);
			outVertex.position.z = static_cast<float>(vControlPoint.mData[2] * scaleFactor);

			outVertex.normal.x = static_cast<float>(vNormalVector.mData[0]);
			outVertex.normal.y = static_cast<float>(vNormalVector.mData[1]);
			outVertex.normal.z = static_cast<float>(vNormalVector.mData[2]);

			outVertex.uv.x = static_cast<float>(UVmap1.mData[0]);
			outVertex.uv.y = static_cast <float>(UVmap1.mData[1]);        

			return outVertex;
        }
    
        // TODO: move to a geomtry helper class
      /*  FbxVector4 GetFbxVector(const DirectX::XMFLOAT4& input)
        {
            return FbxVector4(input.x, input.y, input.z, input.w);
        }

        FbxVector4 GetFbxVector(const DirectX::XMFLOAT3& input)
        {
            return FbxVector4(input.x, input.y, input.z);
        }

        FbxVector2 GetFbxVector(const DirectX::XMFLOAT2& input)
        {
            return FbxVector2(input.x, input.y);
        }        */
    
	};
}