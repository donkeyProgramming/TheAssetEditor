#pragma once

#include "..\DataStructures\PackedMeshStructs.h"
#include "..\Helpers\FBXUnitHelper.h"
#include "..\Logging\Logging.h"
#include "..\Helpers\Geometry\FBXNodeGeometryHelper.h"
#include "..\Helpers\Geometry\FBXMeshGeometryHelper.h"
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
            const std::vector<ControlPointInfluence>& controlPointerInfluences)
        {
            if (!poFbxScene || !poFbxMesh)
                return LogActionError("Mesh processing Failed scene/mesh == nullptr!")

                auto node = poFbxMesh->GetNode();
            auto pMeshName = node->GetName();

            LogAction("Processing Mesh: " + std::string(pMeshName));

            auto fmGloabalTransForm = FBXNodeGeometryHelper::GetNodeWorldTransform(poFbxMesh->GetNode());
            auto fmGloabalTransForm_Normals = FBXNodeGeometryHelper::GetNodeWorldTransformNormals(poFbxMesh->GetNode());

            // -- Total "face corners"
            int polygonVertexCount = poFbxMesh->GetPolygonVertexCount();

            // -- PolygonCount = triangle count, as model is triangulated on load
            int triangleCount = poFbxMesh->GetPolygonCount();
            LogAction("Polygon Count: " + std::to_string(triangleCount));

            // -- Array of "math verticec" {x,y,z(,w)}
            FbxVector4* pControlPoints = poFbxMesh->GetControlPoints();
            int controlPointCount = poFbxMesh->GetControlPointsCount();

            // -- "PolygonVertices" = Polygon "corners", equivalent to "mesh indices"
            int* pPolyggonVertices = poFbxMesh->GetPolygonVertices();


            const int faceIndexCount = 3; // face = harcoded to "triangle" (file is triangulated on FBX init)
            destMesh.indices.resize(triangleCount * faceIndexCount); // unindexes mesh
            destMesh.vertices.resize(triangleCount * faceIndexCount); // indices will be 0,1,2,3,......N			

            FbxGeometryElementNormal::EMappingMode normalMappingMode;

            auto vecNormalsTable = FBXMeshGeometryHelper::GetNormals(poFbxMesh, &normalMappingMode);

            if (vecNormalsTable.empty())  {
                return LogActionError("No normal vectors found!");
            }

            auto textureUVMaps = FBXMeshGeometryHelper::LoadUVInformation(poFbxMesh);

            if (textureUVMaps.size() == 0)  {
                return LogActionError("No UV Maps founds!");
            }

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
                    auto vNormalVector = GetTransformedFBXNormal(normalMappingMode, controlPointIndex, vertexIndex, vecNormalsTable, fmGloabalTransForm_Normals);

                    FbxVector2 uvMap1 = { 0.0, 0.0 };

                    // TODO: should I pick `uv = textureUVMaps["diffuse"]` in stead?
                    if (textureUVMaps.size() > 0)
                    {
                        uvMap1 = (textureUVMaps.begin())->second[vertexIndex]; // at least 1 uv map found, pick the first, 
                    }
                    else
                    {
                        return LogActionError("No UV Maps founds!");
                    }

                    ControlPointInfluence* pControlPointInfluences = nullptr;
                    if (controlPointerInfluences.size() == controlPointCount) // influences from FbxSkin, correst size check
                    {
                        auto const pControlPointInfluences = &controlPointerInfluences[controlPointIndex]; // get the weighting data for all control points

                        for (const auto& i : pControlPointInfluences->influences) // fill "VertexWeights" with "per vertex" weighting data
                        {                            
                            VertexWeight newVertexWeight;


                            strcpy_s<255>(newVertexWeight.boneName, i.boneName);
                            newVertexWeight.vertexIndex = vertexIndex;
                            newVertexWeight.weight = i.weight;

                            destMesh.vertexWeights.push_back(newVertexWeight);
                        }
                    }
                    else
                    {
                        LogActionWarning("Control point influence map SIZE != number of mesh control points");
                    }

                    destMesh.vertices[vertexIndex] = FBXVertexhCreator::MakePackedVertex(v4ControlPoint, vNormalVector, uvMap1, pControlPointInfluences, factorToMeters);
                    destMesh.indices[vertexIndex] = static_cast<uint16_t>(vertexIndex);
                }
            }

            destMesh.meshName = pMeshName;

            LogAction("Done MeshName: " + pMeshName);
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
