#include "FBXMeshCreator.h"

using namespace wrapdll;
using namespace fbxsdk;

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
    fbxsdk::FbxGeometryElementNormal* pGeometryElementNormal = m_poFbxMesh->CreateElementNormal();
    if (!pGeometryElementNormal)
    {
        return LogActionError("Failed to create normal element for mesh: " + inMesh.meshName);
    }

    pGeometryElementNormal->SetMappingMode(FbxGeometryElement::eByPolygonVertex);
    pGeometryElementNormal->SetReferenceMode(FbxGeometryElement::eDirect);

    auto SetVertexNormal = [&](int triangleIndex, int corner)
        {
            fbxsdk::FbxVector4 vNormal;
            auto vertexIndex = inMesh.indices[(3 * triangleIndex) + corner];
            auto normal = inMesh.vertices[vertexIndex].normal;

            vNormal = FbxVector4(-normal.x, normal.y, normal.z);
            pGeometryElementNormal->GetDirectArray().Add(vNormal);
        };

    for (int triangleIndex = 0; triangleIndex < inMesh.indices.size() / 3; triangleIndex++)
    {
        for (int corner = 0; corner < 3; corner++)
        {
            SetVertexNormal(triangleIndex, corner);
        }
    }

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
        m_poFbxMesh->BeginPolygon(); // start triangle(

        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 0]);
        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 1]);
        m_poFbxMesh->AddPolygon(inMesh.indices[3 * i + 2]);

        m_poFbxMesh->EndPolygon(); // end triangle	
    }

    return true;
}
