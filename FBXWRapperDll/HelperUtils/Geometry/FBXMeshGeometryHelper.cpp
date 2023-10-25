#include "FBXMeshGeometryHelper.h"

#include "..\..\Logging\Logging.h"


using namespace wrapdll;

std::map<std::string, std::vector<fbxsdk::FbxVector2>> FBXMeshGeometryHelper::LoadUVInformation(FbxMesh* pMesh)
{
    std::map<std::string, std::vector<fbxsdk::FbxVector2>> uvMap;

    std::map<int, int> test;

    //get all UV set names
    FbxStringList uvSetNameList;
    pMesh->GetUVSetNames(uvSetNameList);
    auto count = pMesh->GetElementUVCount();

    //iterating over all uv sets
    for (int uvSetIndex = 0; uvSetIndex < uvSetNameList.GetCount(); uvSetIndex++)
    {
        //get lUVSetIndex-th uv set
        const char* lUVSetName = uvSetNameList.GetStringAt(uvSetIndex);
        const FbxGeometryElementUV* pUVElement = pMesh->GetElementUV(lUVSetName);

        // init map vector, string -> UV list
        uvMap[lUVSetName] = std::vector<FbxVector2>();

        if (!pUVElement)
            continue;

        // only support mapping mode eByPolygonVertex and eByControlPoint
        if (pUVElement->GetMappingMode() != FbxGeometryElement::eByPolygonVertex &&
            pUVElement->GetMappingMode() != FbxGeometryElement::eByControlPoint)
        {
            continue;
        }

        //index array, where holds the index referenced to the uv data
        const bool bUseIndex = pUVElement->GetReferenceMode() != FbxGeometryElement::eDirect;
        const int indexCount = (bUseIndex) ? pUVElement->GetIndexArray().GetCount() : 0;

        //iterating through the data by polygon
        const int polygonCount = pMesh->GetPolygonCount();

        if (pUVElement->GetMappingMode() == FbxGeometryElement::eByControlPoint)
        {
            for (int lPolyIndex = 0; lPolyIndex < polygonCount; ++lPolyIndex)
            {
                // build the max index array that we need to pass into MakePoly
                const int lPolySize = pMesh->GetPolygonSize(lPolyIndex);
                for (int lVertIndex = 0; lVertIndex < lPolySize; ++lVertIndex)
                {
                    FbxVector2 vUValue;

                    //get the index of the current vertex in control points array
                    int polygonVertex = pMesh->GetPolygonVertex(lPolyIndex, lVertIndex);

                    //the UV index depends on the reference mode
                    int lUVIndex = bUseIndex ? pUVElement->GetIndexArray().GetAt(polygonVertex) : polygonVertex;

                    vUValue = pUVElement->GetDirectArray().GetAt(lUVIndex);

                    // store
                    uvMap[lUVSetName].push_back(vUValue);
                }
            }
        }
        else if (pUVElement->GetMappingMode() == FbxGeometryElement::eByPolygonVertex)
        {
            int polygonIndexCounter = 0;
            for (int polygonIndex = 0; polygonIndex < polygonCount; ++polygonIndex)
            {
                // build the max index array that we need to pass into MakePoly
                const int polygonSize = pMesh->GetPolygonSize(polygonIndex);
                for (int vertexIndex = 0; vertexIndex < polygonSize; ++vertexIndex)
                {
                    if (polygonIndexCounter < indexCount)
                    {
                        FbxVector2 vUVValue;

                        //the UV index depends on the reference mode
                        int uvIndex = bUseIndex ? pUVElement->GetIndexArray().GetAt(polygonIndexCounter) : polygonIndexCounter;

                        vUVValue = pUVElement->GetDirectArray().GetAt(uvIndex);

                        // store
                        uvMap[lUVSetName].push_back(vUVValue);

                        polygonIndexCounter++;
                    }
                }
            }
        }
    }

    return uvMap;
}
//get mesh normals info
std::vector<FbxVector4> FBXMeshGeometryHelper::GetNormals(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode)
{
    std::vector<FbxVector4> fbxNormals;

    if (!poMesh)
    {
        LogActionError("FbxMesh* == NULL");
        return std::vector<FbxVector4>();
    }

    //get the tangent element
    const auto* pVectorElement = poMesh->GetElementNormal();

    if (!pVectorElement)
    {
        LogActionError("FbxGeometryElementTangent* == NULL");
        return std::vector<FbxVector4>();
    }

    if (poMappingMode)
    {
        *poMappingMode = pVectorElement->GetMappingMode();
    }

    return FetchVectors(poMesh, pVectorElement);
}

std::vector<FbxVector4> FBXMeshGeometryHelper::GetTangents(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode)
{
    std::vector<FbxVector4> vecTangents;

    if (!poMesh)
    {
        LogActionError("FbxMesh* == NULL");
        return std::vector<FbxVector4>();
    }

    //get the tangent element
    const fbxsdk::FbxGeometryElementTangent* pVectorElement = poMesh->GetElementTangent();

    if (!pVectorElement)
    {
        LogActionError("FbxGeometryElementTangent* == NULL");
        return std::vector<FbxVector4>();
    }

    if (poMappingMode)
    {
        *poMappingMode = pVectorElement->GetMappingMode();
    }

    return FetchVectors(poMesh, pVectorElement);
}





std::vector<FbxVector4> FBXMeshGeometryHelper::GetBitangents(const fbxsdk::FbxMesh* poMesh, fbxsdk::FbxGeometryElementNormal::EMappingMode* poMappingMode)
{
    std::vector<FbxVector4> vecBinormals;

    if (poMesh)
    {
        //get the normal element
        auto* pVectorElement = poMesh->GetElementBinormal();

        if (pVectorElement)
        {
            if (poMappingMode)
            {
                *poMappingMode = pVectorElement->GetMappingMode();
            }

            return FetchVectors(poMesh, pVectorElement);
        }
    }

    return std::vector<FbxVector4>(); // return empty vector on errors
}

std::vector<FbxVector4> FBXMeshGeometryHelper::FetchVectors(const fbxsdk::FbxMesh* poMesh, const FbxLayerElementTemplate<FbxVector4>* poNormalElement)
{
    std::vector<FbxVector4> fbxVectors;

    if (!poMesh || !poNormalElement)
    {
        return std::vector<FbxVector4>();
        LogActionError("Mesh or NormalElement == NULL");
    }

    // Mapping mode is by Control Points. "The mesh should be smooth and soft."?
    // We can get normals by retrieving each control point
    if (poNormalElement->GetMappingMode() == fbxsdk::FbxGeometryElement::eByControlPoint)
    {
        int controlPointCount = poMesh->GetControlPointsCount();
        fbxVectors.resize(controlPointCount);

        //Let's get normals of each vertex, since the mapping mode of normal element is by control point
        for (int vertexIndex = 0; vertexIndex < poMesh->GetControlPointsCount(); vertexIndex++)
        {
            int lNormalIndex = 0;

            // -- Reference mode is direct, the normal index is same as vertex index.
            // get normals by the index of control vertex
            if (poNormalElement->GetReferenceMode() == fbxsdk::FbxGeometryElement::eDirect)
                lNormalIndex = vertexIndex;

            // -- Reference mode is index-to-direct, get normals by the index-to-direct
            if (poNormalElement->GetReferenceMode() == fbxsdk::FbxGeometryElement::eIndexToDirect)
                lNormalIndex = poNormalElement->GetIndexArray().GetAt(vertexIndex);

            // -- Get Normal using the obtained index
            FbxVector4 lNormal = poNormalElement->GetDirectArray().GetAt(lNormalIndex);
            fbxVectors[vertexIndex] = lNormal;
        }

        return fbxVectors;

    } // End: Vector ByControlPoint

    // Mapping mode is by polygon-vertex.
    // We can get normals by retrieving polygon-vertex.
    if (poNormalElement->GetMappingMode() == FbxGeometryElement::eByPolygonVertex)
    {
        int indexByPolygonVertex = 0;
        int polygon_count = poMesh->GetPolygonCount();
        fbxVectors.clear();
        //Let's get normals of each polygon, since the mapping mode of normal element is by polygon-vertex.
        for (int lPolygonIndex = 0; lPolygonIndex < polygon_count; lPolygonIndex++)
        {
            //get polygon size, you know how many vertices in current polygon, 
            int polygonSize = poMesh->GetPolygonSize(lPolygonIndex);

            if (polygonSize != 3)
            {
                LogActionError("polygonSize != 3, triangles expected..");
                return std::vector<FbxVector4>();
            }

            //retrieve each vertex of current polygon.
            for (int i = 0; i < polygonSize; i++)
            {
                int normalIndex = 0;
                // Reference mode is direct, the normal index is same as lIndexByPolygonVertex.
                if (poNormalElement->GetReferenceMode() == FbxGeometryElement::eDirect)
                    normalIndex = indexByPolygonVertex;

                // Reference mode is index-to-direct, get normals by the index-to-direct
                if (poNormalElement->GetReferenceMode() == FbxGeometryElement::eIndexToDirect)
                    normalIndex = poNormalElement->GetIndexArray().GetAt(indexByPolygonVertex);

                // Got normals of each polygon-vertex.
                FbxVector4 fbxVector4 = poNormalElement->GetDirectArray().GetAt(normalIndex);
                indexByPolygonVertex++;

                fbxVectors.push_back(fbxVector4); // TODO: check that index actually match as  like it does in the bellow out-commented code

                //vecNormals.resize(lIndexByPolygonVertex); // TODO: maybe just use push_back(), if the index will still match
                //vecNormals[lIndexByPolygonVertex - 1L] = lNormal;

            } // for i -> lPolygonSize

        } // lPolygonIndex -> PolygonCount

    } // end eByPolygonVertex


    return fbxVectors;
}


