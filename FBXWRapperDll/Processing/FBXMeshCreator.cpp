#include "FbxMeshCreator.h"
#include "K:\Coding\repos\TheAssetEditor__WITH_FBX_IMPORT\FBXWRapperDll\Helpers\FBXUnitHelper.h"

using namespace fbxsdk;
using namespace wrapdll;

fbxsdk::FbxMesh* FbxMeshCreator::CreateFbxUnindexedMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh, double scaleFactor)
{
    // create fbxnode for mesh   
    auto poFbxMesh = InitFbxMesh(poFbxScene, inMesh);    

    SetControlPoints(poFbxMesh, inMesh, scaleFactor);
    SetNormalVectors(poFbxMesh, inMesh);
    SetTextureCoords(poFbxMesh, inMesh);
    SetPolygonFaces(poFbxMesh, inMesh);

    return poFbxMesh;
}

fbxsdk::FbxMesh* FbxMeshCreator::InitFbxMesh(fbxsdk::FbxScene* poFbxScene, const PackedMesh& inMesh)
{
    return fbxsdk::FbxMesh::Create(poFbxScene, inMesh.meshName.c_str());
}

bool FbxMeshCreator::SetControlPoints(fbxsdk::FbxMesh* poFbxMesh, const PackedMesh& inMesh, double scaleFactor)
{
    poFbxMesh->InitControlPoints(static_cast<int>(inMesh.vertices.size()));
    FbxVector4* pControlPointArray = poFbxMesh->GetControlPoints();

    for (int vertexIndex = 0; vertexIndex < inMesh.vertices.size(); vertexIndex++)
    {
        double x = -inMesh.vertices[vertexIndex].position.x * scaleFactor;
        double y = inMesh.vertices[vertexIndex].position.y * scaleFactor;
        double z = inMesh.vertices[vertexIndex].position.z * scaleFactor;      

        pControlPointArray[vertexIndex].Set(x, y, z);
    };

    return false;
}

bool FbxMeshCreator::SetNormalVectors(fbxsdk::FbxMesh* poFbxMesh, const PackedMesh& inMesh)
{
    fbxsdk::FbxGeometryElementNormal* pGeometryElementNormal = poFbxMesh->CreateElementNormal();
    pGeometryElementNormal->SetMappingMode(FbxGeometryElement::eByPolygonVertex);
    pGeometryElementNormal->SetReferenceMode(FbxGeometryElement::eDirect);

    auto SetVertexNormal =
        [&](int triangleIndex, int corner)
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


    ////int index = 0;
    //for (int i = 0; i < inMesh.vertices.size() / 3; i++)
    //{
    //     fbxsdk::FbxVector4 vNormal;
    //     auto vertex = inMesh.vertices[inMesh.indices[3 * i + 0]];
    //     vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //     
    //     pGeometryElementNormal->GetDirectArray().Add(vNormal);

    //     vertex = inMesh.vertices[inMesh.indices[3 * i + 2]];
    //     vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //     
    //     pGeometryElementNormal->GetDirectArray().Add(vNormal);

    //     vertex = inMesh.vertices[inMesh.indices[3 * i + 1]];
    //     vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);         
    //     pGeometryElementNormal->GetDirectArray().Add(vNormal);


    //// -- unindexed...
    //for (size_t c = 0; c < inMesh.vertices.size(); c++)
    //{
    //    FbxVector4 vNormal;        
    //    auto vertex = inMesh.vertices[c];
    //    vNormal = FbxVector4(-vertex.normal.x, vertex.normal.y, vertex.normal.z);
    //    pGeometryElementNormal->GetDirectArray().Add(vNormal);
    //}

    /*    for (size_t c = 0; c < 3; c++)
        {
            FbxVector4 vNormal;
            auto triangleCorner = inMesh.indices[(3 * i) + c];
            auto vertex = inMesh.vertices[triangleCorner];
            vNormal = FbxVector4(vertex.normal.x, vertex.normal.y, vertex.normal.z);
            pGeometryElementNormal->GetDirectArray()[index++] = vNormal;
        }*/
        //}

    auto temp_DEBUG = pGeometryElementNormal->GetDirectArray().GetCount();

    return false;
}

bool FbxMeshCreator::SetTextureCoords(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh)
{
    // Create UV map 0 storagae element element, 
    // TODO: look into, is string name significant?    

    fbxsdk::FbxGeometryElementUV* poFbxGeometryUV = poMesh->CreateElementUV("DiffuseUV");

    if (poFbxGeometryUV == nullptr)
    {
        return LogActionError((std::string("UV return null, in mesh:") + inMesh.meshName).c_str);
    }

    /*FbxStringList uvNames_DEBUG1;
    pDestFBXMesh->GetUVSetNames(uvNames_DEBUG1);
    auto DEBUG_COUNT_UV1 = pDestFBXMesh->GetElementUVCount();*/

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

bool FbxMeshCreator::SetPolygonFaces(fbxsdk::FbxMesh* poMesh, const PackedMesh& inMesh)
{
    for (int i = 0; i < inMesh.indices.size() / 3; i++)
    {
        poMesh->BeginPolygon(); // start triangle(

        poMesh->AddPolygon(inMesh.indices[3 * i + 0]);
        poMesh->AddPolygon(inMesh.indices[3 * i + 1]);
        poMesh->AddPolygon(inMesh.indices[3 * i + 2]);

        poMesh->EndPolygon(); // end triangle	
    }

    return true;
}
