
#pragma once
#include <fbxsdk.h>
#include <vector>
#include <map>
#include "..\Logging\Logging.h"

namespace wrapdll
{
    class FBXUnitHelper
    {
    public:
        // Inches to meters factor    
        static constexpr double SCALE_FACTOR = (100 / 2.54);    

    public:
        static void SetFbxSystmedUnit(fbxsdk::FbxScene* poFbxScene, fbxsdk::FbxSystemUnit fbxSystemUnit)
        {
            if (!poFbxScene)
            {
                LogActionError("Passed FbxScene == nullptr. Returning stardard factor");
                return;
            }                        

            auto& fbxGlobalSettings = poFbxScene->GetGlobalSettings();
            fbxGlobalSettings.SetSystemUnit(fbxSystemUnit);                     
        }

        /// <summary>
        /// The CA raw position/tranlation data, seem to in cm, but it still requires this factor
        /// The factor it made, so choose any output unit, the scale SHOULD fit
        /// </summary>        
        static double GetDistanceDataScaleFactor(fbxsdk::FbxScene* poFbxScene)
        {
            auto scaleFactorTester = poFbxScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorFrom(fbxsdk::FbxSystemUnit::m);
                        
            return scaleFactorTester;            
        }

        static double GetFactorToMeters(fbxsdk::FbxScene* poFbxScene)
        {
            if (!poFbxScene)
            {
                LogActionError("Passed FbxScene == nullptr. Returning stardard factor");
                return 1.0;
            }

            return poFbxScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorTo(::fbxsdk::FbxSystemUnit::m);
        }

        static double GetFactorFroomMeters(fbxsdk::FbxScene* poFbxScene)
        {
            if (!poFbxScene)
            {
                LogActionError("Passed FbxScene == nullptr. Returning stardard factor");
                return 1.0;
            }

            return poFbxScene->GetGlobalSettings().GetSystemUnit().GetConversionFactorFrom(::fbxsdk::FbxSystemUnit::m);
        }

        static fbxsdk::FbxGlobalSettings* GetGlobalSetting(fbxsdk::FbxScene* poFbxScene)
        {
            if (!poFbxScene)
            {
                LogActionError("Passed FbxScene == nullptr. Returning stardard factor");
                return nullptr;
            }

            return &poFbxScene->GetGlobalSettings();
        }   

        static std::string GetUnitAsString(fbxsdk::FbxScene* poFbxScene)
        {
            if (!poFbxScene)
            {
                LogActionError("Passed FbxScene == nullptr. Returning stardard empty string");
                return "";
            }

            return poFbxScene->GetGlobalSettings().GetSystemUnit().GetScaleFactorAsString_Plurial().Buffer();
        }
    };
}