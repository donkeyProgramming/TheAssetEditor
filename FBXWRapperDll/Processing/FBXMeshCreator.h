#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"
#include "..\Logging\Logging.h"
#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"
#include "..\Helpers\Geometry\FBXMeshGeometryHelper.h"
//#include "..\Helpers\SimpleMath\SimpleMath.h"
//#include "..\Helpers\VectorConverter.h"
#include "FBXVertexCreator.h"


namespace wrapdll
{
	class FBXMeshCreator
	{
	public:
		static bool MakeUnindexPackedMesh(
			fbxsdk::FbxScene* poFbxScene,
			fbxsdk::FbxMesh* poFbxMesh,
			PackedMesh& destMesh,
			std::vector<ControlPointInfluences> controlPointerInfluences)
		{
			if (!poFbxScene || !poFbxMesh)
				return log_action_error("Mesh processing Failed scene/mesh == nullptr!")

								auto node = poFbxMesh->GetNode();
			auto pMeshName = node->GetName();

			log_action("Processing Mesh: " + std::string(pMeshName));

			auto fmGloabalTransForm = FBXNodeGeometryHelper::GetNodeWorldTransform(poFbxMesh->GetNode());
			auto fmGloabalTransForm_Normals = FBXNodeGeometryHelper::GetNodeWorldTransform_Normals(poFbxMesh->GetNode());

			// Total "face corners"
			int polygonVertexCount = poFbxMesh->GetPolygonVertexCount();

			// PolygonCount = triangle count, as model is triangulated on init
			int triangleCount = poFbxMesh->GetPolygonCount();
			log_action("Polygon Count: " + std::to_string(triangleCount));

			// Array of "math verticec" {x,y,z(,w)}
			FbxVector4* pControlPoints = poFbxMesh->GetControlPoints();
			int controlPointCount = poFbxMesh->GetControlPointsCount();

			// "PolygonVertices" = Polygon "corners", equivalent to "mesh indices"
			int* pPolyggonVertices = poFbxMesh->GetPolygonVertices();

			const int faceIndexCount = 3; // face = harcoded to "triangle" (file is triangulated on FBX init)
			destMesh.indices.resize(triangleCount * faceIndexCount);
			destMesh.vertices.resize(triangleCount * faceIndexCount);

			//poFbxMesh->GenerateTangentsDataForAllUVSets(); // not "pretty" once converted to "INDEXED packed vertex", recalc for perfection

			FbxGeometryElementNormal::EMappingMode NormalMappingMode;

			auto vecNormalsTable = FBXMeshGeometryHelper::GetNormals(poFbxMesh, &NormalMappingMode);

			if (vecNormalsTable.empty())
				return log_action_error("No normal vectors found!")

				auto textureUVMaps = FBXMeshGeometryHelper::LoadUVInformation(poFbxMesh);

			if (textureUVMaps.size() == 0)
				return log_action_error("No UV Maps founds!");

			// obtain the standard scaling factor to meters

			// TODO: find out if you can eliminate the need for the weird x 39. thing, or not
			// ------- weird factor used in "make vertex" elsewhere
			// cm -> inces: 1/2.54 = 0.39
			// m -> cm: *100
			// m -> inches = 39.
			auto factorToMeters = FBXUnitHelper::GetFactorToMeters(poFbxScene);

			for (int faceIndex = 0; faceIndex < triangleCount; faceIndex++)
			{
				for (int faceCorner = 0; faceCorner < faceIndexCount; faceCorner++)
				{
					auto vertexIndex = (faceIndex * faceIndexCount) + faceCorner;
					auto& destVertexRef = destMesh.vertices[vertexIndex];

					// -- FBC "control point" = ("pure" math verrtex, position only),
					int controlPointIndex = poFbxMesh->GetPolygonVertex(faceIndex, faceCorner);

					// transform position an and normal, by node transforms
					FbxVector4 v4ControlPoint = GetPositionTransformedFBX(pControlPoints, controlPointIndex, fmGloabalTransForm);
					auto vNormalVector = GetTransformedFBXNormal(NormalMappingMode, controlPointIndex, vertexIndex, vecNormalsTable, fmGloabalTransForm_Normals);

					FbxVector2 uvMap1 = { 0.0, 0.0 };

					// TODO: should I pick `uv = textureUVMaps["diffuse"]` in stead?
					if (textureUVMaps.size() > 0)
					{
						uvMap1 = (textureUVMaps.begin())->second[vertexIndex]; // at least 1 uv map found, pick the first, 
					}
					else
					{
						return log_action_error("No UV Maps founds!");
					}

					ControlPointInfluences* pControlPointInfluences = nullptr;
					if (controlPointerInfluences.size() == controlPointCount) // influences from FbxSkin, correst size check
					{
						pControlPointInfluences = &controlPointerInfluences[controlPointIndex];
					}

					destMesh.vertices[vertexIndex] = FBXVertexhCreator::MakePackedVertex(v4ControlPoint, vNormalVector, uvMap1, pControlPointInfluences, factorToMeters);
					destMesh.indices[vertexIndex] = static_cast<uint16_t>(vertexIndex);
				}
			}

			destMesh.meshName = pMeshName;

			log_action("Done MeshName: " + pMeshName);
			return true;
		}

		static FbxVector4 GetTransformedFBXNormal(FbxGeometryElementNormal::EMappingMode NormalMappingMode, int  controlPointIndex, int vertexIndex, const std::vector < fbxsdk::FbxVector4>& m_vecNormals, FbxAMatrix transform)
		{
			FbxVector4 vNormalVector = FbxVector4(0, 0, 0, 0);
			if (NormalMappingMode == FbxGeometryElementNormal::EMappingMode::eByControlPoint)
			{
				vNormalVector = m_vecNormals[controlPointIndex];
			}
			else if (NormalMappingMode == FbxGeometryElementNormal::EMappingMode::eByPolygonVertex)
			{
				vNormalVector = m_vecNormals[vertexIndex];
			}

			vNormalVector = (transform).MultT(vNormalVector);
			vNormalVector.Normalize();
			return vNormalVector;
		}

		static FbxVector4 GetPositionTransformedFBX(FbxVector4* pControlPoints, int controlPointIndex, FbxAMatrix& transform)
		{
			FbxVector4 v4ControlPoint = pControlPoints[controlPointIndex];
			return transform.MultT(v4ControlPoint);
		};
	};
}
