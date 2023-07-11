#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"

namespace wrapdll{

	class FBXVertexhCreator
	{
	public:
		static constexpr PackedCommonVertex MakePackedVertex(
			const FbxVector4& vControlPoint,
			const FbxVector4& vNormalVector,
			const FbxVector2& UVmap1,
			const ControlPointInfluences* ctrlPointInfluences,
			double scaleFactor)
		{
			PackedCommonVertex destVertexRef;

			destVertexRef.position.x = static_cast<float>(vControlPoint.mData[0] * scaleFactor);
			destVertexRef.position.y = static_cast<float>(vControlPoint.mData[1] * scaleFactor);
			destVertexRef.position.z = static_cast<float>(vControlPoint.mData[2] * scaleFactor);

			destVertexRef.normal.x = static_cast<float>(vNormalVector.mData[0]);
			destVertexRef.normal.y = static_cast<float>(vNormalVector.mData[1]);
			destVertexRef.normal.z = static_cast<float>(vNormalVector.mData[2]);

			destVertexRef.uv.x = static_cast<float>(UVmap1.mData[0]);
			destVertexRef.uv.y = static_cast <float>(UVmap1.mData[1]);

			if (ctrlPointInfluences)
			{
				destVertexRef.weightCount = ctrlPointInfluences->weightCount;

				for (size_t i = 0; i < destVertexRef.weightCount; i++)
				{
					destVertexRef.influences[i].boneIndex = ctrlPointInfluences->influences[i].boneIndex;
					destVertexRef.influences[i].weight = ctrlPointInfluences->influences[i].weight;
				}
			}
			else
				destVertexRef.weightCount = 0;

			return destVertexRef;
		};
	};
}