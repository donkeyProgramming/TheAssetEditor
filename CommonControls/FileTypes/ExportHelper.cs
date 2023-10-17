// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.WsModel;
using CommonControls.Interfaces.AssetManagement;
using CommonControls.Services;

namespace CommonControls.Events.UiCommands
{

    /// <summary>
    ///  Helper class to fetch WSMODEL/RMV2/ANIM files, and extract data
    /// </summary>
    public class ExportHelper
    {

        private const string SkeletonDir = "animations/skeletons/";

        private const string WSModelExtenstion = ".wsmodel";
        private const string RMV2Extenstion = ".rigid_model_v2";

        /// <summary>
        /// Custom "Get Extension"
        /// As the built-in one seems to "go foward", so "stuff.more.txt", become "more.txt"
        /// </summary>
        static public string GetLastExtension(string path)
        {
            string extension = "";
            bool anyDotsFound = false;
            for (int i = path.Length - 1; i > 0; i--)
            {
                extension = extension.Insert(0, path[i].ToString());
                if (path[i] == '.')
                {
                    anyDotsFound = true;
                    break;
                }
            }

            return anyDotsFound ? extension : "";
        }

        /// <summary>
        /// Fetches the input files in parsed version
        /// </summary>
        /// <param name="packFileService">service to look "pathModel" in</param>
        /// <param name="pathModel">pathModel in .pack path</param>
        /// <param name="pathAnimationClip">Animations in .pack path</param>
        /// <returns></returns>
        static public AssetManagerData FetchParsedInputFiles(PackFileService packFileService, string pathModel, string pathAnimationClip = "")
        {
            var inputData = new AssetManagerData();

            GetModel(packFileService, pathModel, inputData);

            if (inputData.RigidModelFile.Header.SkeletonName.Any())
            {
                string pathSkeleton = $"{SkeletonDir}{inputData.RigidModelFile.Header.SkeletonName}.anim";

                inputData.skeletonFile = GetAnimationFile(packFileService, pathSkeleton);
            }

            if (pathAnimationClip.Any())
            {
                inputData.animationFile = GetAnimationFile(packFileService, pathAnimationClip);
            }            

            return inputData;
        }

        private static AssetManagerData GetModel(PackFileService packFileService, string pathModel, AssetManagerData inputData)
        {
            var inputPathExtension = GetLastExtension(pathModel);
            switch (inputPathExtension.ToLower())
            {
                case WSModelExtenstion:
                    {
                        inputData.InputPackFile = GetPackFile(packFileService, pathModel);
                        inputData.wsmodelFile = new WsMaterial[1] { GetWSModel(inputData.InputPackFile) };
                        inputData.RigidModelFile = GetRMV2FromWSModel(packFileService, inputData.wsmodelFile[0]);
                    }
                    break;

                case RMV2Extenstion:
                    {
                        inputData.wsmodelFile = null; // no WSMODEL
                        inputData.RigidModelFile = GetRMV2File(packFileService, pathModel);
                    }
                    break;

                default:
                    throw new Exception("GetModel: invalid file extensions.");

            };

            return inputData;
        }

        static public AnimationFile GetAnimationFile(PackFileService packFileService, string animPackFilePath)
        {
            if (animPackFilePath == "")
                return null;

            var animPackFile = GetPackFile(packFileService, animPackFilePath);

            var animationFile = (animPackFile != null) ? AnimationFile.Create(animPackFile) : null;

            return animationFile;
        }

        static public RmvFile GetRMV2File(PackFileService _packFileService, string pathRMV2)
        {
            // TODO:TEST            
            try
            {
                var rmv2PackFile = _packFileService.FindFile(pathRMV2);
                if (rmv2PackFile == null)
                    return null;

                RmvFile tempRmv2 = new RmvFile();
                var modelFactory = ModelFactory.Create();
                var rmv2File = modelFactory.Load(rmv2PackFile.DataSource.ReadData());

                return rmv2File;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Getting of {pathRMV2} causes error : {e.Message}", "Error");
                return null;
            }
        }

        static public WsMaterial GetWSModel(PackFile wsModelPackFile)
        {
            var wsModel = new WsMaterial(wsModelPackFile);
            return wsModel;
        }

        static public RmvFile GetRMV2FromWSModel(PackFileService packFileService, WsMaterial wsModelFile)
        {
            return GetRMV2File(packFileService, wsModelFile.GeometryPath);
        }


        /// <summary>
        ///  For fetching file in .pack in simple, neat way
        /// </summary>        
        static public PackFile GetPackFile(PackFileService packFileService, string packFilePath)
        {
            try
            {
                var packFile = packFileService.FindFile(packFilePath);

                return packFile;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Getting PackFile {packFilePath} caused error : {e.Message}", "Serious Error");
                return null;
            }
        }

        List<IMaterial> GetMaterialFromRmv2(RmvFile rmv2File)
        {
            if (rmv2File.ModelList.Length == 0 || rmv2File.ModelList[0].Length != 0)
            {
                return null;
            }

            var materials = new List<IMaterial>();
            {
                foreach (var model in rmv2File.ModelList[0])
                {
                    materials.Add(model.Material);
                }
            }

            return materials;
        }


        // TODO: add methods
        /*
        public
         - (RmvFile, Material) GetModelWithMaterial(string modelPath)   // for RMV2 or WSMODEL
         OR
         - Material GetMaterial(Model)         

        private
         - RmvFile GetModelFromWsModelFile()
         - Material GetMaterialFromRmvFile()         
         - Material GetMaterialFromWsModelFile()
        */
    }
}

