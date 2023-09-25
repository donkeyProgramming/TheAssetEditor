// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.WsModel;
using CommonControls.Services;

namespace CommonControls.Events.UiCommands
{
    public class ExportHelper
    {
        static public string GetLastExtension(string path)
        {
            string extension = "";
            bool anyDotsFound = false;
            for (int i = path.Length; i > 0; i--)
            {
                extension.Insert(0, path[i].ToString());
                if (path[i] == '.')
                {
                    anyDotsFound = true;
                    break;
                }
            }

            return anyDotsFound ? extension : "";
        }

        static public InputData FetchInputFiles(PackFileService packFileService, string pathModel, string pathAnimationClip = "")
        {
            var inputData = new InputData();

            inputData = GetModel(packFileService, pathModel, inputData);

            string rmv2SkeletonId = inputData.RigidModelFile.Header.SkeletonName;
            string pathSkeleton = $"skeletons/{rmv2SkeletonId}.anim";
            inputData.skeletonFile = GetAnimationFile(packFileService, pathSkeleton);
            inputData.animationFile = GetAnimationFile(packFileService, pathAnimationClip);


            return inputData;
        }

        private static InputData GetModel(PackFileService packFileService, string pathModel, InputData inputData)
        {
            var inputPathExtension = GetLastExtension(pathModel);
            switch (inputPathExtension.ToLower())
            {
                case "wsmodel":
                    {
                        var packFile = GetPackFile(packFileService, pathModel);
                        inputData.wsmodelFile = GetWSModel(packFile);
                        inputData.RigidModelFile = GetRMV2FromWSModel(packFileService, inputData.wsmodelFile);
                    }
                    break;

                case "rigid_model_v2":
                    {
                        inputData.wsmodelFile = null;// TODO:finsih
                        inputData.RigidModelFile = GetRMV2File(packFileService, pathModel);
                    }
                    break;

            };

            return inputData;
        }

        static public AnimationFile GetAnimationFile(PackFileService packFileService, string animPackFilePath)
        {
            if (animPackFilePath == "")
                return null;

            var animPackFile = GetPackFile(packFileService, animPackFilePath);
            AnimationFile animationFile = null;
            if (animPackFile != null)
                animationFile = AnimationFile.Create(animPackFile);

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
                if (packFile == null)
                    return null;

                return packFile;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Getting PackFile {packFilePath} caused error : {e.Message}", "Serious Error");
                return null;
            }
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

