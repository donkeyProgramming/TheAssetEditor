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
        FbxString materialName = name.c_str();
        FbxString shadingName = "Phong";

        FbxDouble3 colorBlack(0.0, 0.0, 0.0);
        FbxDouble3 colorRed(1.0, 0.0, 0.0);
        FbxVector4 basicColor(152, 178, 245, 255); // gotten from "online color picker"
        basicColor /= 255.0; // get into 0..1 vaues

        FbxSurfacePhong* material = FbxSurfacePhong::Create(poFbxScene, (name + "--mat").c_str());

        // Generate primary and secondary colors.
        material->Emissive.Set(colorBlack);

        //lMaterial->Ambient.Set(colorRed);
        material->Ambient.Set(colorRed);

        material->Diffuse.Set(basicColor);
        material->TransparencyFactor.Set(0.0);        
        material->ShadingModel.Set(shadingName);
        material->Shininess.Set(0.1);

        return material;
    }
};

