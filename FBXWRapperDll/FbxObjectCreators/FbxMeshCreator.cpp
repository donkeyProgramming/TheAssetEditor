#include "FBXMeshCreator.h"
#include "..\..\Logging\Logging.h"
#include "..\DataStructures\PackedMeshStructs.h"

// TODO: remove, once test it done
#include "..\HelperUtils\Geometry\FBXMeshGeometryHelper.h"



using namespace wrapdll;
using namespace fbxsdk;

static bool AlmostEqualToOne(float weight)
{
    return weight > 0.95f && weight < 1.05f ? true : false;
}

static void CheckForWeightErrors(const PackedMesh& outMesh)
{
    for (auto testVertexIndex = 0; testVertexIndex < outMesh.vertices.size(); testVertexIndex++)
    {
        auto testWeight = 0.0f;

        size_t weightIndex = 0;
        for (; weightIndex < outMesh.vertexWeights.size(); weightIndex++) // sum all weights with with the current vertex index            
        {
            if (outMesh.vertexWeights[weightIndex].vertexIndex == testVertexIndex)
            {
                testWeight += outMesh.vertexWeights[weightIndex].weight;
            }
        }

        if (!AlmostEqualToOne(testWeight))
        {
            throw std::exception("vertex is null-weighted");
        }
    }
}


fbxsdk::FbxMesh* FbxMeshUnindexedCreator::Create(
    fbxsdk::FbxScene* poFbxScene,
    const PackedMesh& inMesh,
    SceneContainer& sceneContainer)
{
    InitFbxMesh(poFbxScene, inMesh);

    SetControlPoints(inMesh, sceneContainer.GetDistanceScaleFactor());
    SetNormalVectors(inMesh);
    SetTextureCoords(inMesh);
    SetPolygonFaces(inMesh);

    return m_poFbxMesh;
}

bool FbxMeshUnindexedCreator::InitFbxMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh)
{
    auto localPrinter = TimeLogAction::PrintStart("Creating FbxMesh: " + inMesh.meshName);
    m_poFbxMesh = fbxsdk::FbxMesh::Create(poFbxScene, inMesh.meshName.c_str());

    if (!m_poFbxMesh)
    {
        return LogActionError("Failed to create mesh: " + inMesh.meshName);
    }
    LogAction("Creating FBXMesh. Done.: " + inMesh.meshName);

    localPrinter.PrintDone();

    return true;
}

bool FbxMeshUnindexedCreator::SetControlPoints(const PackedMesh& inMesh, double scaleFactor)
{
    m_poFbxMesh->InitControlPoints(static_cast<int>(inMesh.vertices.size()));
    FbxVector4* pControlPointArray = m_poFbxMesh->GetControlPoints();

    for (int vertexIndex = 0; vertexIndex < inMesh.vertices.size(); vertexIndex++)
    {
        double x = -inMesh.vertices[vertexIndex].position.x * scaleFactor;
        double y = inMesh.vertices[vertexIndex].position.y * scaleFactor;
        double z = inMesh.vertices[vertexIndex].position.z * scaleFactor;

        pControlPointArray[vertexIndex].Set(x, y, z);
    };

    return true;
}

bool FbxMeshUnindexedCreator::SetNormalVectors(const PackedMesh& inMesh)
{
    // TODO: CLEAN UP !!! Once it works
    // sets normals by triangle corner, so each triangle has 3 normals stored,      
    fbxsdk::FbxGeometryElementNormal* pGeometryElementNormal = m_poFbxMesh->CreateElementNormal();
    pGeometryElementNormal->SetMappingMode(FbxGeometryElement::eByPolygonVertex);
    pGeometryElementNormal->SetReferenceMode(FbxGeometryElement::eDirect);

    auto AddNormal = [&](size_t vertexIndex)
        { 
            auto vertex = inMesh.vertices[vertexIndex];
            FbxVector4 vNormal(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
            pGeometryElementNormal->GetDirectArray().Add(vNormal);
        };
    
    for (int i = 0; i < inMesh.indices.size() / 3; i++)
    {
        // reverse wind order, needed
        AddNormal(inMesh.indices[3 * i + 0]);
        AddNormal(inMesh.indices[3 * i + 2]);
        AddNormal(inMesh.indices[3 * i + 1]);
    }

    // unindex mesh, so vertices can be used in sequence
    //for (int i = 0; i < vecVertices.size() / 3; i++)
    //{
    //    FbxVector4 vNormal;
    //    auto vertex = vecVertices[3 * i + 0];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);

    //    vertex = vecVertices[3 * i + 2];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);

    //    vertex = vecVertices[3 * i + 1];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);
    //}


    return true;

    //for (int i = 0; i < vecIndices.size() / 3; i++)
    //{
    //    FbxVector4 vNormal;
    //    auto vertex = vecVertices[vecIndices[3 * i + 0]];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);

    //    vertex = vecVertices[vecIndices[3 * i + 2]];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);               

    //    vertex = vecVertices[vecIndices[3 * i + 1]];        
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);       
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);
    //}

    //return true;







    //fbxsdk::FbxGeometryElementNormal* pGeometryElementNormal = m_poFbxMesh->CreateElementNormal();
    //if (!pGeometryElementNormal)
    //{
    //    return LogActionError("Failed to create normal element for mesh: " + inMesh.meshName);
    //}

    //pGeometryElementNormal->SetMappingMode(FbxGeometryElement::eByPolygonVertex);
    //pGeometryElementNormal->SetReferenceMode(FbxGeometryElement::eDirect);

    ///*if (pGeometryElementNormal->GetDirectArray().SetCount(inMesh.indices.size()))
    //{
    //    return LogActionError("Normal vector memory alloc error for mesh: " + inMesh.meshName);
    //}*/

    ///*auto addNormal = [&](const PackedCommonVertex& vertex)
    //    {            
    //        FbxVector4 vNormal(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //        pGeometryElementNormal->GetDirectArray().Add(vNormal);
    //    };*/

    //int DEBUG_NORMAL_SIZE = 0;
    //for (const auto& cornerIndex : inMesh.indices)
    //{
    //    //addNormal(inMesh.vertices[cornerIndex]);
    //    auto vertex = inMesh.vertices[cornerIndex];
    //    FbxVector4 vNormal(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);


    //    DEBUG_NORMAL_SIZE = pGeometryElementNormal->GetDirectArray().GetCount();
    //    auto debug_1 = 1;
    //}

    //// TODO: remove
    //FbxGeometryElement::EMappingMode DEBUG_mmpaingMode;
    //auto DEBUG_normal = FBXMeshGeometryHelper::GetNormals(this->m_poFbxMesh, &DEBUG_mmpaingMode);


    ////auto SetVertexNormal = [&](int triangleIndex, int corner)
    ////    {
    ////        fbxsdk::FbxVector4 vNormal;
    ////        auto vertexIndex = inMesh.indices[(3 * triangleIndex) + corner];
    ////        auto normal = inMesh.vertices[vertexIndex].normal;

    ////        vNormal = FbxVector4(-normal.x, normal.y, normal.z);
    ////        pGeometryElementNormal->GetDirectArray().Add(vNormal);
    ////    };

    ////for (int triangleIndex = 0; triangleIndex < inMesh.indices.size() / 3; triangleIndex++)
    ////{
    ////    for (int corner = 0; corner < 3; corner++)
    ////    {
    ////        SetVertexNormal(triangleIndex, corner);
    ////    }
    ////}

    return true;
}

bool FbxMeshUnindexedCreator::SetTextureCoords(const PackedMesh& inMesh)
{
    // Create UV map 0 storagae element element, 
    // TODO: look into, is string name significant?    

    fbxsdk::FbxGeometryElementUV* poFbxGeometryUV = m_poFbxMesh->CreateElementUV("DiffuseUV");

    if (poFbxGeometryUV == nullptr)
    {
        return LogActionError("UV error: fbxsdk::CreateElementUV returned null , in mesh:" + inMesh.meshName);
    }

    poFbxGeometryUV->SetMappingMode(FbxGeometryElement::eByControlPoint);
    poFbxGeometryUV->SetReferenceMode(FbxGeometryElement::eDirect);

    for (int i = 0; i < inMesh.vertices.size(); i++)
    {
        // TODO: is this inversion needed?
        FbxVector2 vUV1(
            inMesh.vertices[i].uv.x,
            1.0 - inMesh.vertices[i].uv.y);

        poFbxGeometryUV->GetDirectArray().Add(vUV1);
    }

    return true;
}

bool FbxMeshUnindexedCreator::SetPolygonFaces(const PackedMesh& inMesh)
{
    for (int i = 0; i < inMesh.indices.size() / 3; i++)
    {
        m_poFbxMesh->BeginPolygon(); // start triangle

        // reverse wind order, needed
        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 0]);
        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 2]);
        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 1]);

        m_poFbxMesh->EndPolygon(); // end triangle	
    }

    return true;
}
