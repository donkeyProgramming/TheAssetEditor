#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"

namespace wrapdll{

	class FBXVertexhCreator
	{
	public:
		static PackedCommonVertex MakePackedVertex(
			const FbxVector4& vControlPoint,
			const FbxVector4& vNormalVector,
			const FbxVector2& UVmap1,
			const ControlPointInfluenceExt* ctrlPointInfluences,
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

			if (ctrlPointInfluences)
			{
				outVertex.weightCount = ctrlPointInfluences->weightCount;

				for (size_t i = 0; i < outVertex.weightCount; i++)
				{
					strcpy_s<255>(outVertex.influences[i].boneName, ctrlPointInfluences->influences[i].boneName.c_str());
					outVertex.influences[i].boneIndex = ctrlPointInfluences->influences[i].boneIndex;
					outVertex.influences[i].weight = ctrlPointInfluences->influences[i].weight;
				}
			}
			else
				outVertex.weightCount = 0;

			return outVertex;
		};
	};
}