#pragma once

#include <fbxsdk.h>
#include "..\DataStructures\PackedMeshStructs.h"

class IFbxMaterialCreator
{
public:
    virtual fbxsdk::FbxSurfaceMaterial* CreateMaterial(
        fbxsdk::FbxScene* poFbxScene,
        const std::string& name,
        const FbxDouble3& color1,
        const FbxDouble3& color2,
        const FbxDouble3& color3,
        const FbxDouble& transparencyFactory) = 0;
};


class FbxMaterialPhongCreator : public IFbxMaterialCreator
{
public:
    virtual fbxsdk::FbxSurfaceMaterial* CreateMaterial(
        fbxsdk::FbxScene* poFbxScene,
        const std::string& name,
        const FbxDouble3& diffuseColor = { 0.0, 0.0, 0.0 },
        const FbxDouble3& emmissiveColor = { 0.0, 0.0, 0.0 },
        const FbxDouble3& ambientColor = { 0.0, 0.0, 0.0 }, 
        const FbxDouble& transparencyFactory = 0.0) override
    {
        FbxString lMaterialName = name.c_str();
        FbxString lShadingName = "Phong";

        FbxDouble3 colorBlack(0.0, 0.0, 0.0);
        FbxDouble3 colorRed(1.0, 0.0, 0.0);
        FbxVector4 basicColor(60, 197, 232, 255);
        basicColor /= 255.0;

        FbxSurfacePhong* lMaterial = FbxSurfacePhong::Create(poFbxScene, (name + "--mat").c_str());

        // Generate primary and secondary colors.
        lMaterial->Emissive.Set(colorBlack);

        //lMaterial->Ambient.Set(colorRed);
        lMaterial->Ambient.Set(basicColor);               

        lMaterial->Diffuse.Set(basicColor);
        lMaterial->TransparencyFactor.Set(0.0);        
        lMaterial->ShadingModel.Set(lShadingName);
        lMaterial->Shininess.Set(0.5);

        return lMaterial;
    }

    /*
     - create an FbxMaterial with the meshe's name + "_mat"
     - assign some please default shading for the material, like light blue or something
     - return FbxMaterial
    */
};

